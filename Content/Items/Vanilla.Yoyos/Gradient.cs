using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Content.Particles;
using SPYoyoMod.Core.Graphics;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class GradientAssets : ILoadable
    {
        public const string InvisiblePath = $"{_assetPath}Invisible";

        public static Asset<Effect> GodraysEffect { get; private set; } = ModContent.Request<Effect>($"{_yoyoPath}GradientEffect_Godrays", AssetRequestMode.ImmediateLoad); //< Immediate здесь обязателен

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Gradient/";

        void ILoadable.Unload()
        {
            GodraysEffect = null;
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class GradientItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Gradient;
    }

    public sealed class GradientProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Gradient;

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            var godraysType = ModContent.ProjectileType<GradientGodraysProjectile>();

            if (proj.GetOwner().ownedProjectileCounts[godraysType] > 0)
                return;

            foreach (var otherProj in Main.ActiveProjectiles)
            {
                if (otherProj.type != godraysType)
                    continue;

                if ((otherProj.As<GradientGodraysProjectile>().Target?.whoAmI ?? -1) == target.whoAmI)
                    return;
            }

            Projectile.NewProjectile(proj.GetSource_OnHit(proj), target.Center, Vector2.Zero, ModContent.ProjectileType<GradientGodraysProjectile>(), proj.damage, proj.knockBack, proj.owner, target.whoAmI);
        }
    }

    public sealed class GradientGodraysProjectile : ModProjectile, IInitializableProjectile, IEmitLightProjectile
    {
        public static readonly int InitTimeLeft = ModUtils.SecondsToTicks(3f);
        public static readonly EasingBuilder OpacityEasing = new(
            (EasingFunctions.InOutQuad, 0.07f, 0f, 1f),
            (EasingFunctions.Linear, 0.78f, 1f, 1f),
            (EasingFunctions.InOutQuad, 0.15f, 1f, 0f)
        );

        private StripRenderer _stripRenderer;

        public override string Texture { get => GradientAssets.InvisiblePath; }
        public NPC Target { get => (int)Projectile.ai[0] >= 0 ? Main.npc[(int)Projectile.ai[0]] : null; }
        public float LifeTimeRatio { get => 1f - Projectile.timeLeft / (float)InitTimeLeft; }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = TileUtils.TileSizeInPixels * 105;
        }

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;

            Projectile.timeLeft = InitTimeLeft;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
        }

        void IInitializableProjectile.Initialize(Projectile proj)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _stripRenderer = new StripRenderer(Main.graphics.GraphicsDevice, 2);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _stripRenderer?.Dispose();
        }

        public override void AI()
        {
            if (Target is null || !Target.active)
            {
                var deathTimeLeft = (int)(InitTimeLeft * 0.15f); //< 1.00 - (0.07 + 0.78) из OpacityEasing

                if (Projectile.timeLeft > deathTimeLeft)
                    Projectile.timeLeft = deathTimeLeft;

                return;
            }

            Projectile.Center = Target.Center;

            if (LifeTimeRatio > 0.75f)
                return;

            if (Projectile.timeLeft % 4 == 0)
            {
                var particle = WorldParticleManager.SpawnParticle<LightPointParticle>();

                particle.LifeTime = ModUtils.SecondsToTicks(1.5f);
                particle.Position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-TileUtils.TileSizeInPixels * 6, TileUtils.TileSizeInPixels * 6), Main.rand.NextFloat(-TileUtils.TileSizeInPixels * 70, 6));
                particle.Velocity = Main.rand.NextVector2Circular(0.2f, 0.5f);
                particle.StartColor = new Color(255, 250, 185);
                particle.EndColor = new Color(255, 190, 0);
                particle.Scale = Main.rand.NextFloat(0.2f, 0.3f);
            }
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        void IEmitLightProjectile.EmitLight(Projectile _)
        {
            var opacity = OpacityEasing.Evaluate(LifeTimeRatio);

            if (opacity <= 0f)
                return;

            Lighting.AddLight(Projectile.Center, new Color(255, 190, 0).ToVector3() * opacity * 0.5f);
        }

        public override void PostDraw(Color lightColor)
        {
            if (_stripRenderer is null)
                return;

            var opacity = OpacityEasing.Evaluate(LifeTimeRatio);

            if (opacity <= 0f)
                return;

            var position = Projectile.Bottom + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var lightningStartPosition = position - Vector2.UnitY * TileUtils.TileSizeInPixels * 100;
            var lightningEndPosition = position;

            GradientAssets.GodraysEffect
                .Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(TextureAssets.MagicPixel.Value);
                    parameters["TransformMatrix"].SetValue(GameMatrices.Transform * GameMatrices.Projection);
                    parameters["Position"].SetValue(Projectile.Bottom);
                    parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                    parameters["Opacity"].SetValue(opacity);
                })
                .Apply();

            _stripRenderer
                .SetWidth(TileUtils.TileSizeInPixels * 16)
                .SetPoints([lightningStartPosition, lightningEndPosition])
                .Render();
        }
    }

    public sealed class GradientDaggerProjectile : ModProjectile
    {
        public override string Texture { get => GradientAssets.InvisiblePath; }
    }
}