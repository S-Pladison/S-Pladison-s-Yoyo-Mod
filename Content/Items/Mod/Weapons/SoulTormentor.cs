using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class SoulTormentorItem : YoyoItem
    {
        public override string Texture { get => ModAssets.ItemsPath + "SoulTormentor"; }

        public SoulTormentorItem() : base(gamepadExtraRange: 15) { }

        public override void YoyoSetDefaults()
        {
            Item.damage = 43;
            Item.knockBack = 2.5f;
            Item.autoReuse = true;

            Item.shoot = ModContent.ProjectileType<SoulTormentorProjectile>();

            Item.rare = ItemRarityID.Lime;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    public class SoulTormentorProjectile : YoyoProjectile, IDrawPixelatedProjectile, IDrawPixelatedPrimitivesProjectile
    {
        public override string Texture { get => ModAssets.ProjectilesPath + "SoulTormentor"; }

        private TrailRenderer blackTrailRenderer;
        private TrailRenderer redTrailRenderer;
        private LineRenderer lineRenderer;

        public SoulTormentorProjectile() : base(lifeTime: -1f, maxRange: 300f, topSpeed: 13f) { }

        public override void AI()
        {
            if (Main.rand.NextBool(2))
            {
                var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                var position = Projectile.Center + vector * Main.rand.NextFloat(7f, 18f);
                var velocity = vector * Main.rand.NextFloat(0.5f, 3f);

                Dust.NewDustPerfect(position, ModContent.DustType<SoulTormentorDust>(), velocity);
            }

            blackTrailRenderer?.SetNextPoint(Projectile.Center + Projectile.velocity);
            redTrailRenderer?.SetNextPoint(Projectile.Center + Projectile.velocity + new Vector2(6f, 0).RotatedBy(MathHelper.Pi + Projectile.rotation));
        }

        void IDrawPixelatedProjectile.PreDrawPixelated(Projectile _)
        {
            var drawPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/SoulTormentor_Ring", AssetRequestMode.ImmediateLoad);
            var scale = Projectile.scale;

            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.Black, 0f, texture.Size() * 0.5f, 0.47f * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, new(255, 0, 35), 0f, texture.Size() * 0.5f, 0.5f * scale, SpriteEffects.None, 0f);
        }

        void IDrawPixelatedPrimitivesProjectile.PreDrawPixelatedPrimitives(Projectile _, PrimitiveMatrices matrices)
        {
            blackTrailRenderer ??= InitTrailRenderer(25, 16);
            redTrailRenderer ??= InitTrailRenderer(20, 8);
            lineRenderer ??= InitLineRenderer();

            var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "DefaultPrimitive", AssetRequestMode.ImmediateLoad);
            var effect = effectAsset.Value;
            var effectParameters = effect.Parameters;

            effectParameters["Texture0"].SetValue(TextureAssets.MagicPixel.Value);
            effectParameters["TransformMatrix"].SetValue(matrices.TransformWithScreenOffset);

            var blackColorVec4 = Color.Black.ToVector4();

            effectParameters["ColorTL"].SetValue(blackColorVec4);
            effectParameters["ColorTR"].SetValue(blackColorVec4);
            effectParameters["ColorBL"].SetValue(blackColorVec4);
            effectParameters["ColorBR"].SetValue(blackColorVec4);

            blackTrailRenderer.Draw(effect);

            var redColorVec4 = new Color(255, 0, 35).ToVector4();

            effectParameters["ColorTL"].SetValue(redColorVec4);
            effectParameters["ColorTR"].SetValue(redColorVec4);
            effectParameters["ColorBL"].SetValue(redColorVec4);
            effectParameters["ColorBR"].SetValue(redColorVec4);

            redTrailRenderer.Draw(effect);

            var targets = NPCUtils.NearestNPCs
            (
                center: Projectile.Center,
                radius: 16 * 10,
                predicate: (npc) => npc.CanBeChasedBy(Projectile, false) || npc.type.Equals(NPCID.TargetDummy)
            );

            var projectilePos = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY;

            /*foreach (var (npc, distance) in targets)
            {
                //var progress = 1f - MathF.Pow(target.distance / EFFECT_DRAW_RADIUS, 2.5f);
                var progress = 1f;
                var npcPos = npc.Center + npc.gfxOffY * Vector2.UnitY;
                var npcDrawPos = npcPos - Main.screenPosition;
                var sin = MathF.Sin(Main.GlobalTimeWrappedHourly * 2f + progress + npc.whoAmI);
                var normal = Vector2.Normalize(Projectile.Center - npc.Center).RotatedBy(MathHelper.PiOver2) * sin * 16 * 3;
                var points = new List<Vector2>() { projectilePos, projectilePos + normal, npcPos - normal, npcPos };
                var bezierPoints = BezierCurve.GetPoints(8, points);

                lineRenderer.SetPoints(bezierPoints).Draw(effect);
            }*/
        }

        private TrailRenderer InitTrailRenderer(int pointCount, float width)
            => new TrailRenderer(pointCount).SetWidth(f => MathHelper.Lerp(width, 0f, f));

        private LineRenderer InitLineRenderer()
        {
            return new LineRenderer(8, false);
        }
    }

    public class SoulTormentorDust : ModDust
    {
        public override string Texture { get => ModAssets.DustsPath + "SoulTormentor"; }

        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.frame = new Rectangle(Main.rand.Next(4) * 18, 0, 18, 18);
            dust.rotation = Main.rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
            dust.scale *= Main.rand.NextFloat(1f, 1.2f);
            dust.color = Color.White;
        }

        public override bool PreDraw(Dust dust)
        {
            Main.spriteBatch.Draw(Texture2D.Value, dust.position - Main.screenPosition, new Rectangle?(dust.frame), dust.color, dust.rotation, new Vector2(9, 9), dust.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}