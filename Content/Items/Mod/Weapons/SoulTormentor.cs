using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.PixelatedLayers;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class SoulTormentorItem : YoyoItem
    {
        public override string Texture => ModAssets.ItemsPath + "SoulTormentor";
        public override int GamepadExtraRange => 15;

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

    public class SoulTormentorProjectile : YoyoProjectile
    {
        public static readonly float TormentorRadius = 16 * 15;
        public static readonly int TormentorCount = 3;

        public override string Texture => ModAssets.ProjectilesPath + "SoulTormentor";
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;

        private TrailRenderer blackTrailRenderer;
        private TrailRenderer redTrailRenderer;

        public override void OnKill(int timeLeft)
        {
            blackTrailRenderer?.Dispose();
            redTrailRenderer?.Dispose();
        }

        public override void AI()
        {
            foreach ((var npc, var _) in GetTargets())
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
            foreach ((var npc, var _) in GetTargets())
            {
                if (target.whoAmI == npc.whoAmI)
                    continue;

                var hitInfo = hit;
                hitInfo.HitDirection = Math.Sign((npc.Center - owner.Center).X);
                hitInfo.Damage /= 2;
                hitInfo.Knockback /= 2;

                npc.StrikeNPC(hitInfo);

                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendStrikeNPC(npc, in hitInfo);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            blackTrailRenderer ??= InitTrailRenderer(25, 16);
            redTrailRenderer ??= InitTrailRenderer(20, 8);

            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.UnderProjectiles, () =>
            {
                var effect = ModAssets.RequestEffect("DefaultStrip").Prepare(parameters =>
                {
                    var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "StripGradient_BlackToAlpha_PremultipliedAlpha", AssetRequestMode.ImmediateLoad);

                    parameters["Texture0"].SetValue(texture.Value);
                    parameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.TransformWithScreenOffset);

                    var blackColorVec4 = Color.Black.ToVector4();

                    parameters["ColorTL"].SetValue(blackColorVec4);
                    parameters["ColorTR"].SetValue(blackColorVec4);
                    parameters["ColorBL"].SetValue(blackColorVec4);
                    parameters["ColorBR"].SetValue(blackColorVec4);
                });

                blackTrailRenderer?.Draw(effect);

                effect.Prepare(parameters =>
                {
                    var redColorVec4 = new Color(255, 0, 35).ToVector4();

                    parameters["ColorTL"].SetValue(redColorVec4);
                    parameters["ColorTR"].SetValue(redColorVec4);
                    parameters["ColorBL"].SetValue(redColorVec4);
                    parameters["ColorBR"].SetValue(redColorVec4);
                });

                redTrailRenderer?.Draw(effect);

                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "SoulTormentor_Ring", AssetRequestMode.ImmediateLoad);
                var drawPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
                var scale = Projectile.scale;

                Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.Black, 0f, texture.Size() * 0.5f, 0.47f * scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture.Value, drawPosition, null, new(255, 0, 35), 0f, texture.Size() * 0.5f, 0.5f * scale, SpriteEffects.None, 0f);
            });

            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.OverProjectiles, () =>
            {
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Heart", AssetRequestMode.ImmediateLoad);
                var origin = texture.Size() * 0.5f;

                foreach ((var npc, var distance) in GetTargets())
                {
                    var npcPos = npc.Center + npc.gfxOffY * Vector2.UnitY;
                    var npcDrawPos = npcPos - Main.screenPosition;
                    var rotation = MathF.Sin((float)Main.timeForVisualEffects * 0.03f + npc.whoAmI);
                    var color = Color.Lerp(Color.Transparent, new Color(255, 0, 35), EasingFunctions.OutExpo(1f - distance / TormentorRadius) * 2f);

                    Main.spriteBatch.Draw(texture.Value, npcDrawPos, null, color, rotation, origin, 0.25f, SpriteEffects.None, 0);
                }
            });

            return true;
        }

        private List<(NPC npc, float distance)> GetTargets()
        {
            return NPCUtils.NearestNPCs(Projectile.Center, TormentorRadius, (npc) => npc.CanBeChasedBy(Projectile, false) || npc.type.Equals(NPCID.TargetDummy))
                .Take(TormentorCount)
                .ToList();
        }

        private void SpawnDusts(Vector2 position)
        {
            var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
            var velocity = vector * Main.rand.NextFloat(0.5f, 3f);

            position += vector * Main.rand.NextFloat(7f, 18f);

            Dust.NewDustPerfect(position, ModContent.DustType<SoulTormentorDust>(), velocity);
        }

        private TrailRenderer InitTrailRenderer(int pointCount, float width)
        {
            return new TrailRenderer(pointCount).SetWidth(f => MathHelper.Lerp(width, 0f, f));
        }
    }

    public class SoulTormentorDust : ModDust
    {
        public override string Texture => ModAssets.DustsPath + "SoulTormentor";

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