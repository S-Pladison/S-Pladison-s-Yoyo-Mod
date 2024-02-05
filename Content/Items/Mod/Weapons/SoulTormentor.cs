using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
            Item.width = 42;
            Item.height = 26;

            Item.damage = 43;
            Item.knockBack = 2.5f;
            Item.autoReuse = true;

            Item.shoot = ModContent.ProjectileType<SoulTormentorProjectile>();

            Item.rare = ItemRarityID.Lime;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    public class SoulTormentorProjectile : YoyoProjectile, IDrawPixelatedProjectile
    {
        public static readonly float TormentorRadius = 16 * 15;
        public static readonly int TormentorCount = 3;

        public override string Texture { get => ModAssets.ProjectilesPath + "SoulTormentor"; }

        private TrailRenderer blackTrailRenderer;
        private TrailRenderer redTrailRenderer;

        public SoulTormentorProjectile() : base(lifeTime: -1f, maxRange: 300f, topSpeed: 13f) { }

        public override void OnKill(int timeLeft)
        {
            blackTrailRenderer?.Dispose();
            redTrailRenderer?.Dispose();
        }

        public override void AI()
        {
            foreach (var (npc, _) in GetTargets())
            {
                if (Main.rand.NextBool(12))
                    SpawnDusts(npc.Center);
            }

            if (Main.rand.NextBool(3))
                SpawnDusts(Projectile.Center);

            blackTrailRenderer?.SetNextPoint(Projectile.Center + Projectile.velocity);
            redTrailRenderer?.SetNextPoint(Projectile.Center + Projectile.velocity + new Vector2(6f, 0).RotatedBy(MathHelper.Pi + Projectile.rotation));
        }

        public override void YoyoOnHitNPC(Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            foreach (var (npc, _) in GetTargets())
            {
                if (target.whoAmI == npc.whoAmI)
                    continue;

                var hitInfo = hit;
                hitInfo.HitDirection = Math.Sign((npc.Center - owner.Center).X);
                hitInfo.Damage /= 2;
                hitInfo.Knockback /= 2;

                npc.StrikeNPC(hitInfo);
            }
        }

        private List<(NPC npc, float distance)> GetTargets()
            => NPCUtils.NearestNPCs(Projectile.Center, TormentorRadius, (npc) => npc.CanBeChasedBy(Projectile, false) || npc.type.Equals(NPCID.TargetDummy)).Take(TormentorCount).ToList();

        private void SpawnDusts(Vector2 position)
        {
            var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
            var velocity = vector * Main.rand.NextFloat(0.5f, 3f);

            position += vector * Main.rand.NextFloat(7f, 18f);

            Dust.NewDustPerfect(position, ModContent.DustType<SoulTormentorDust>(), velocity);
        }

        void IPreDrawPixelatedProjectile.PreDrawPixelated(Projectile _)
        {
            blackTrailRenderer ??= InitTrailRenderer(25, 16);
            redTrailRenderer ??= InitTrailRenderer(20, 8);

            var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "DefaultPrimitive", AssetRequestMode.ImmediateLoad);
            var effect = effectAsset.Value;
            var effectParameters = effect.Parameters;

            effectParameters["Texture0"].SetValue(TextureAssets.MagicPixel.Value);
            effectParameters["TransformMatrix"].SetValue(ProjectileDrawLayers.PixelatedPrimitiveMatrices.TransformWithScreenOffset);

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

            var drawPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/SoulTormentor_Ring", AssetRequestMode.ImmediateLoad);
            var scale = Projectile.scale;

            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.Black, 0f, texture.Size() * 0.5f, 0.47f * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, new(255, 0, 35), 0f, texture.Size() * 0.5f, 0.5f * scale, SpriteEffects.None, 0f);
        }

        void IPostDrawPixelatedProjectile.PostDrawPixelated(Projectile _)
        {
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Heart", AssetRequestMode.ImmediateLoad);
            var origin = texture.Size() * 0.5f;

            foreach (var (npc, distance) in GetTargets())
            {
                var npcPos = npc.Center + npc.gfxOffY * Vector2.UnitY;
                var npcDrawPos = npcPos - Main.screenPosition;
                var rotation = MathF.Sin((float)Main.timeForVisualEffects * 0.03f + npc.whoAmI);
                var color = Color.Lerp(Color.Transparent, new Color(255, 0, 35), EasingFunctions.OutExpo(1f - distance / TormentorRadius) * 2f);

                Main.spriteBatch.Draw(texture.Value, npcDrawPos, null, color, rotation, origin, 0.25f, SpriteEffects.None, 0);
            }
        }

        private TrailRenderer InitTrailRenderer(int pointCount, float width)
            => new TrailRenderer(pointCount).SetWidth(f => MathHelper.Lerp(width, 0f, f));
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