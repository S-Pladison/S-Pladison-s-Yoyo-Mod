using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.PixelatedLayers;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Content.Dusts;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Entities;
using SPYoyoMod.Utils.Rendering;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class HelFireItem : VanillaYoyoItem
    {
        public override int YoyoType => ItemID.HelFire;
    }

    public class HelFireProjectile : VanillaYoyoProjectile
    {
        private static Lazy<Effect> trailEffect;

        public override int YoyoType => ProjectileID.HelFire;

        private TrailRenderer trailRenderer;

        public override void Load()
        {
            if (Main.dedServ) return;

            trailEffect = new Lazy<Effect>(() =>
            {
                var asset = ModContent.Request<Effect>(ModAssets.EffectsPath + "HelFireTrail", AssetRequestMode.ImmediateLoad);
                var effect = asset.Value;
                var parameters = effect.Parameters;
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "FireStrip", AssetRequestMode.ImmediateLoad);

                parameters["Texture0"].SetValue(texture.Value);
                parameters["Color0"].SetValue(new Color(255, 210, 140).ToVector4());
                parameters["Color1"].SetValue(new Color(240, 150, 185).ToVector4());
                parameters["Color2"].SetValue(new Color(115, 145, 255).ToVector4());
                parameters["Color3"].SetValue(new Color(80, 0, 255).ToVector4());

                return effect;
            });
        }

        public override void Unload()
        {
            trailEffect = null;
        }

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            Projectile.NewProjectile(proj.GetSource_FromThis(), proj.Center, Vector2.Zero, ModContent.ProjectileType<HelFireDiscProjectile>(), proj.damage, proj.knockBack, proj.owner, proj.identity);
        }

        public override void OnKill(Projectile proj, int timeLeft)
        {
            trailRenderer?.Dispose();
        }

        public override void AI(Projectile proj)
        {
            if (proj.velocity.Length() >= 3f && Main.rand.NextBool(4))
            {
                var dustColor = Color.Lerp(new Color(255, 210, 140), new Color(115, 145, 255), Main.rand.NextFloat(0, 1));
                var dustIndex = Dust.NewDust(proj.position + Vector2.One * 4, proj.width - 2, proj.height - 2, ModContent.DustType<CircleGlowDust>(), 0f, 0f, 0, dustColor, Main.rand.NextFloat(0.5f, 0.9f));
                var dust = Main.dust[dustIndex];

                dust.velocity *= 0.4f;
            }

            trailRenderer?.SetNextPoint(proj.Center + proj.velocity);
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            DrawUtils.DrawYoyoString(proj, mountedCenter, (segmentCount, segmentIndex, position, rotation, height, color) =>
            {
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "FishingLine_WithShadow", AssetRequestMode.ImmediateLoad);
                var pos = position - Main.screenPosition;
                var rect = new Rectangle(0, 0, texture.Width(), (int)height);
                var origin = new Vector2(texture.Width() * 0.5f, 0f);
                var colour = Color.Lerp(Color.Transparent, Color.Lerp(new Color(115, 145, 255), new Color(255, 210, 140), segmentIndex / (float)segmentCount), EasingFunctions.InQuart(segmentIndex / (float)segmentCount) * 5f);

                Main.spriteBatch.Draw(texture.Value, pos, rect, colour, rotation, origin, 1f, SpriteEffects.None, 0f);
            });
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            trailRenderer ??= new TrailRenderer(13).SetWidth(f => 34f);

            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.UnderProjectiles, () =>
            {
                if (trailRenderer is null) return;

                var length = trailRenderer.GetTotalLengthBetweenPoints();

                if (length <= 0f) return;

                var effect = trailEffect.Value;
                var effectParameters = effect.Parameters;

                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "FireStrip", AssetRequestMode.ImmediateLoad);
                var uvRepeat = length / texture.Width() * 0.75f;

                effectParameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.TransformWithScreenOffset);
                effectParameters["UvRepeat"].SetValue(uvRepeat);
                effectParameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.05f);

                trailRenderer.Draw(effect);
            });

            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Yoyo_GlowWithShadow", AssetRequestMode.ImmediateLoad);

            Main.spriteBatch.Draw(texture.Value, position, null, new Color(255, 210, 140), proj.rotation, texture.Size() * 0.5f, proj.scale * 1.2f, SpriteEffects.None, 0f);

            texture = TextureAssets.Projectile[YoyoType];

            Main.spriteBatch.Draw(texture.Value, position, null, Color.White, proj.rotation, texture.Size() * 0.5f, proj.scale, SpriteEffects.None, 0);

            return false;
        }
    }

    public class HelFireDiscProjectile : ModProjectile
    {
        public const int HitboxRadius = 40;
        public const float DiscOffsetRadius = 16 * 7f;
        public const float TargetRadius = DiscOffsetRadius * 2f;

        public override string Texture { get => ModAssets.MiscPath + "Invisible"; }
        public int YoyoProjIdentity { get => (int)Projectile.ai[0]; }

        private bool initialized;
        private int yoyoProjIndex;
        private bool useYoyoCenter;
        private RingRenderer ringRenderer;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = HitboxRadius * 2;
            Projectile.height = HitboxRadius * 2;

            Projectile.timeLeft = 2;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;

            Projectile.netImportant = true;
        }

        public override void OnKill(int timeLeft)
        {
            ringRenderer?.Dispose();
        }

        public override void AI()
        {
            if (!initialized)
            {
                yoyoProjIndex = Main.projectile.FirstOrDefault(p => p.identity == YoyoProjIdentity && p.type == ProjectileID.HelFire)?.whoAmI ?? -1;
                useYoyoCenter = true;
                initialized = true;
            }

            if (yoyoProjIndex < 0)
            {
                Projectile.Kill();
                return;
            }

            var yoyoProj = Main.projectile[yoyoProjIndex];

            if (yoyoProjIndex < 0
                || yoyoProj.type != ProjectileID.HelFire
                || !yoyoProj.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft += 1;

            var targets = NPCUtils.NearestNPCs(
               center: yoyoProj.Center,
               radius: TargetRadius,
               predicate: (npc) =>
                   npc.CanBeChasedBy(yoyoProj, false) ||
                   npc.type == NPCID.TargetDummy
            );

            var anyTarget = targets.Count > 0;

            if (anyTarget)
            {
                useYoyoCenter = false;

                Projectile.MoveTo(targets[0].npc.Center, 24f, 0.15f);

                var vectorFromYoyoToProj = Projectile.Center - yoyoProj.Center;
                var vectorFromYoyoToProjLength = vectorFromYoyoToProj.Length();

                if (vectorFromYoyoToProjLength > DiscOffsetRadius)
                {
                    Projectile.Center = yoyoProj.Center + Vector2.Normalize(vectorFromYoyoToProj) * DiscOffsetRadius;
                }

                return;
            }

            if (!useYoyoCenter)
            {
                if (!yoyoProj.Hitbox.Contains(Projectile.Center.ToPoint()))
                {
                    Projectile.MoveTo(yoyoProj.Center, 18f, 1f);
                    return;
                }

                useYoyoCenter = true;
            }

            Projectile.Center = yoyoProj.Center;
            Projectile.velocity = Vector2.Zero;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            var projCenter = projHitbox.Center.ToVector2();
            var vectorToTarget = Vector2.Normalize(targetHitbox.Center.ToVector2() - projCenter);

            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), projCenter, projCenter + vectorToTarget * HitboxRadius);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, Main.rand.Next(60 * 3, 60 * 8));
            Main.player[Projectile.owner].Counterweight(target.Center, Projectile.damage, Projectile.knockBack);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (yoyoProjIndex < 0) return false;

            ringRenderer ??= new RingRenderer(20, 16f * 3f, 16f * 3f);

            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.OverProjectiles, () =>
            {
                var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "CascadeExplosionRing", AssetRequestMode.ImmediateLoad);
                var effect = effectAsset.Value;
                var effectParameters = effect.Parameters;

                effectParameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.MiscPath + "FireStrip", AssetRequestMode.ImmediateLoad).Value);
                effectParameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.TransformWithScreenOffset);
                effectParameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.05f);
                effectParameters["UvRepeat"].SetValue(3f);
                effectParameters["Color0"].SetValue(new Color(115, 145, 255).ToVector4());
                effectParameters["Color1"].SetValue(new Color(80, 0, 255).ToVector4());

                var thickness = 16f;
                var radius = HitboxRadius - thickness * 0.5f;

                ringRenderer?
                    .SetThickness(thickness)
                    .SetRadius(radius)
                    .SetPosition(Projectile.Center + Projectile.gfxOffY * Vector2.UnitY)
                    .Draw(effect);
            });

            var texture = TextureAssets.Chain6;

            var projPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY;
            var yoyoProj = Main.projectile[yoyoProjIndex];
            var yoyoPosition = yoyoProj.Center + yoyoProj.gfxOffY * Vector2.UnitY;

            var vectorToYoyo = yoyoPosition - projPosition;
            var vectorToYoyoLength = (int)vectorToYoyo.Length();

            var segmentRotation = vectorToYoyo.ToRotation() + MathHelper.PiOver2;
            var segmentOrigin = texture.Size() * 0.5f;
            var segmentCount = (int)Math.Ceiling((float)vectorToYoyoLength / texture.Width());
            var segmentVector = Vector2.Normalize(vectorToYoyo) * texture.Width();

            for (var i = 0; i < segmentCount; i++)
            {
                var position = projPosition + segmentVector * i - Main.screenPosition;
                var color = Color.Lerp(Color.White, Color.Transparent, i / (float)segmentCount);

                Main.spriteBatch.Draw(texture.Value, position, null, color, segmentRotation, segmentOrigin, 1f, SpriteEffects.None, 0);
            }

            return true;
        }
    }
}