using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class ChikAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Chik/Chik";

        public const string InvisiblePath = $"{AssetPath}/Invisible";
        public const string StringPath = $"{AssetPath}/FishingLine_WithShadow";

        public static readonly LazyAsset<Texture2D> GlowTexture = LazyAsset<Texture2D>.From($"{AssetPath}/YoyoGlow_WithShadow");
    }

    public sealed class ChikItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Chik;
    }

    public sealed class ChikProjectile : VanillaYoyoBaseProjectile, IInitializableProjectile
    {
        public static readonly Color GlowColor = new(55, 160, 255);

        private YoyoStringRenderer _stringRenderer;

        public override int ProjType => ProjectileID.Chik;
        public override bool InstancePerEntity => true;

        void IInitializableProjectile.Initialize(Projectile _)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _stringRenderer = new YoyoStringRenderer(new IDrawYoyoStringSegments.Gradient(
               ModContent.Request<Texture2D>(ChikAssets.StringPath, AssetRequestMode.ImmediateLoad).Value,
               (Color.Transparent, true), (Color.Transparent, true), (GlowColor, true)
            ));
        }

        public override void AI(Projectile proj)
        {
            proj.rotation -= 0.15f;

            Lighting.AddLight(proj.Center, GlowColor.ToVector3() * 0.2f);
        }

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 5; i++)
            {
                Projectile.NewProjectile(proj.GetSource_OnHit(target), proj.Center, Vector2.Zero, ModContent.ProjectileType<ChikHomingProjectile>(), proj.damage, proj.knockBack, proj.owner, target.whoAmI);
            }
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            if (_stringRenderer is null)
                return;

            var settings = new YoyoStringRendererSettings(
                proj: proj,
                start: mountedCenter + proj.GetOwner()?.gfxOffY * Vector2.UnitY ?? Vector2.Zero,
                offset: -Main.screenPosition
            );

            _stringRenderer.Render(Main.spriteBatch, settings);
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            var glowPosition = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var glowTexture = ChikAssets.GlowTexture.Value;
            var glowOrigin = glowTexture.Size() * 0.5f;
            var glowScale = proj.scale * 1.2f;

            Main.spriteBatch.Draw(glowTexture, glowPosition, null, GlowColor, proj.rotation, glowOrigin, glowScale, SpriteEffects.None, 0f);

            return true;
        }
    }

    public sealed class ChikHomingProjectile : ModProjectile, IInitializableProjectile
    {
        private Vector2 _spawnPosition;
        private Vector2 _controlPoint1;
        private Vector2 _controlPoint2;
        private Vector2 _controlPoint3;
        private Vector2 _targetPosition;

        public override string Texture => ChikAssets.InvisiblePath;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = 16;
            Projectile.height = 16;

            Projectile.timeLeft = 60 * 2;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;

            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 0;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _spawnPosition = Projectile.Center;
            _controlPoint1 = _spawnPosition + Main.rand.NextVector2CircularEdge(500f, 500f);
            _controlPoint2 = _spawnPosition + Main.rand.NextVector2CircularEdge(800f, 800f);
            _controlPoint3 = _spawnPosition + Main.rand.NextVector2CircularEdge(500f, 500f);
            _targetPosition = Projectile.Center;

            Main.NewText(_spawnPosition);
        }

        void IInitializableProjectile.Initialize(Projectile proj)
        {

        }

        public override void AI()
        {
            float lifeTimeRatio = 1f - Projectile.timeLeft / (60 * 2f);
            Projectile.Center = BezierCurve.Evaluate(lifeTimeRatio, _spawnPosition, _controlPoint1, _controlPoint2, _controlPoint3, _targetPosition);
        }

        public override bool? CanHitNPC(NPC target)
        {
            float lifeTimeRatio = 1f - Projectile.timeLeft / (60f * 2f);

            if (lifeTimeRatio < 0.8f)
                return false;

            return base.CanHitNPC(target);
        }

        /*public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_spawnPosition.X);
            writer.Write(_spawnPosition.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _spawnPosition.X = reader.ReadSingle();
            _spawnPosition.Y = reader.ReadSingle();
        }*/
    }
}