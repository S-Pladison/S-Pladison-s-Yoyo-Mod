using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System;
using System.Collections.Generic;
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

    public class SoulTormentorProjectile : YoyoProjectile, IDrawPixelatedPrimitivesProjectile
    {
        public override string Texture { get => ModAssets.ProjectilesPath + "SoulTormentor"; }

        private TrailRenderer trailRenderer;
        private LineRenderer lineRenderer;

        public SoulTormentorProjectile() : base(lifeTime: -1f, maxRange: 300f, topSpeed: 13f) { }

        public override void AI()
        {
            //Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SoulTormentorDust>(), Vector2.Zero);

            trailRenderer?.SetNextPoint(Projectile.Center + Projectile.velocity);
        }

        void IDrawPixelatedPrimitivesProjectile.PreDrawPixelatedPrimitives(Projectile _, PrimitiveMatrices matrices)
        {
            trailRenderer ??= InitTrailRenderer();
            lineRenderer ??= InitLineRenderer();

            var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "DefaultPrimitive", AssetRequestMode.ImmediateLoad);
            var effect = effectAsset.Value;
            var effectParameters = effect.Parameters;

            effectParameters["Texture0"].SetValue(TextureAssets.MagicPixel.Value);
            effectParameters["ColorTL"].SetValue(Color.White.ToVector4());
            effectParameters["ColorTR"].SetValue(Color.White.ToVector4());
            effectParameters["ColorBL"].SetValue(Color.White.ToVector4());
            effectParameters["ColorBR"].SetValue(Color.White.ToVector4());
            effectParameters["TransformMatrix"].SetValue(matrices.TransformWithScreenOffset);

            trailRenderer ??= InitTrailRenderer();
            trailRenderer.Draw(effect);

            var targets = NPCUtils.NearestNPCs
            (
                center: Projectile.Center,
                radius: 16 * 10,
                predicate: (npc) => npc.CanBeChasedBy(Projectile, false) || npc.type.Equals(NPCID.TargetDummy)
            );

            var projectilePos = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY;

            foreach (var (npc, distance) in targets)
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
            }
        }

        private TrailRenderer InitTrailRenderer()
        {
            return new TrailRenderer(15);
        }

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
            dust.frame = new Rectangle(0, Main.rand.Next(2) * 18, 18, 18);
            dust.color = new Color[] { new(255, 0, 35), new(225, 40, 120) }[Main.rand.Next(2)];
        }

        public override bool PreDraw(Dust dust)
        {
            Main.spriteBatch.Draw(Texture2D.Value, dust.position - Main.screenPosition, new Rectangle?(dust.frame), dust.color, dust.rotation, new Vector2(9, 9), dust.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}