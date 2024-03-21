using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Graphics.PixelatedLayers;
using SPYoyoMod.Common.Graphics.Renderers;
using SPYoyoMod.Content.Dusts;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Rendering;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class CascadeItem : VanillaYoyoItem
    {
        public override int YoyoType => ItemID.Cascade;

        public override void AddRecipes()
        {
            Recipe.Create(ItemID.Cascade)
                .AddIngredient(ItemID.HellstoneBar, 15)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    public class CascadeProjectile : VanillaYoyoProjectile
    {
        public const float StartToChargeTime = 60 * 2f;
        public const float ExplodeTime = StartToChargeTime + 60 * 0.7f;

        private static readonly EasingBuilder trailWidthEasing = new(
            (EasingFunctions.OutExpo, 0.05f, 0f, 30f),
            (EasingFunctions.Linear, 0.95f, 30f, 25f)
        );

        public override int YoyoType => ProjectileID.Cascade;

        private TrailRenderer trailRenderer;
        private int timer;

        public override void AI(Projectile proj)
        {
            timer++;

            if (proj.velocity.Length() >= 3f && Main.rand.NextBool(4))
            {
                var dustIndex = Dust.NewDust(proj.position + Vector2.One * 4, proj.width - 2, proj.height - 2, ModContent.DustType<CircleGlowDust>(), 0f, 0f, 0, new Color(255, 135, 10));
                var dust = Main.dust[dustIndex];

                dust.velocity *= 0.4f;
            }

            if (timer >= StartToChargeTime)
            {
                var chargeProgress = (timer - StartToChargeTime) / (ExplodeTime - StartToChargeTime);

                if (chargeProgress < 0.01f)
                {
                    SoundEngine.PlaySound(new SoundStyle(ModAssets.SoundsPath + "CascadeBeforeExplosion"), proj.Center);
                }
                else if (chargeProgress < 0.7f)
                {
                    var position = proj.Center + proj.velocity * 2;
                    var vectorToDust = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));

                    Dust.NewDustPerfect(position + vectorToDust * 16 * 5, ModContent.DustType<CircleGlowDust>(), -vectorToDust * 5, 0, new Color(255, 135, 10), Main.rand.NextFloat(0.5f, 1f));
                }
                else if (chargeProgress >= 1f)
                {
                    if (Main.myPlayer == proj.owner)
                    {
                        Projectile.NewProjectile(proj.GetSource_FromAI(), proj.Center, Vector2.Zero, ModContent.ProjectileType<CascadeExplosionProjectile>(), proj.damage, proj.knockBack, proj.owner);
                    }

                    SoundEngine.PlaySound(SoundID.Item14, proj.Center);

                    timer = 0;
                }
            }

            trailRenderer?.SetNextPoint(proj.Center + proj.velocity);

            Lighting.AddLight(proj.Center, new Color(255, 180, 95).ToVector3() * 0.25f);
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            DrawUtils.DrawGradientYoyoStringWithShadow(proj, mountedCenter, (Color.Transparent, true), (new Color(255, 180, 95), true));
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            trailRenderer ??= new TrailRenderer(15, f => trailWidthEasing.Evaluate(f));

            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.UnderProjectiles, () =>
            {
                if (trailRenderer is null) return;

                var length = trailRenderer.Points.DistanceBetween();

                if (length <= 0f) return;

                trailRenderer.Draw(ModAssets.RequestEffect("CascadeTrail").Prepare(parameters =>
                {
                    var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "FireStrip", AssetRequestMode.ImmediateLoad);

                    parameters["Texture0"].SetValue(texture.Value);
                    parameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.TransformWithScreenOffset);
                    parameters["Color0"].SetValue(new Color(255, 255, 160).ToVector4());
                    parameters["Color1"].SetValue(new Color(255, 80, 0).ToVector4());
                    parameters["Color2"].SetValue(new Color(250, 50, 100).ToVector4());
                    parameters["Color3"].SetValue(new Color(70, 30, 150).ToVector4());
                    parameters["UvRepeat"].SetValue(length / texture.Width());
                    parameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.05f);
                }));
            });

            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Yoyo_GlowWithShadow", AssetRequestMode.ImmediateLoad);
            var color = new Color(255, 180, 95);

            Main.spriteBatch.Draw(texture.Value, position, null, color, proj.rotation, texture.Size() * 0.5f, proj.scale * 1.2f, SpriteEffects.None, 0f);

            return true;
        }

        public override void PostDraw(Projectile proj, Color lightColor)
        {
            if (timer >= StartToChargeTime && timer <= ExplodeTime)
            {
                var chargeProgress = (timer - StartToChargeTime) / (ExplodeTime - StartToChargeTime);

                var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Circle", AssetRequestMode.ImmediateLoad);
                var color = new Color(255, 135, 0) with { A = 0 } * chargeProgress * 0.2f;

                Main.spriteBatch.Draw(texture.Value, position, null, color, proj.rotation, texture.Size() * 0.5f, proj.scale * 0.3f, SpriteEffects.None, 0f);

                texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Yoyo_GlowWithShadow", AssetRequestMode.ImmediateLoad);
                color = Color.White with { A = 0 } * chargeProgress;

                Main.spriteBatch.Draw(texture.Value, position, null, color, proj.rotation, texture.Size() * 0.5f, proj.scale * 1.2f, SpriteEffects.None, 0f);
            }
        }
    }

    public class CascadeExplosionProjectile : ModProjectile
    {
        public const int MaxRadius = 16 * 6;
        public const int InitTimeLeft = 20;

        public override string Texture => ModAssets.MiscPath + "Invisible";
        public float TimeLeftProgress => 1f - Projectile.timeLeft / (float)InitTimeLeft;

        private bool initialized;
        private RingRenderer ringRenderer;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = MaxRadius * 2;
            Projectile.height = MaxRadius * 2;

            Projectile.timeLeft = InitTimeLeft;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnKill(int timeLeft)
        {
            ringRenderer?.Dispose();
        }

        public override void AI()
        {
            if (!initialized)
            {
                var dustType = ModContent.DustType<SmokeDust>();

                for (int i = 0; i < 15; i++)
                {
                    var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                    var position = Projectile.Center + vector * Main.rand.NextFloat(MaxRadius * 0.75f);
                    var velocity = vector * Main.rand.NextFloat(1f, 3f);
                    var dust = Dust.NewDustPerfect(position, dustType, velocity, Main.rand.Next(50, 100), Color.White, Main.rand.NextFloat(0.2f, 0.3f));
                    dust.customData = new SmokeDust.CustomData(new Color(255, 140, 20), true, new Color(50, 50, 50), false);

                    vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                    position = Projectile.Center + vector * Main.rand.NextFloat(MaxRadius * 0.75f);
                    velocity = vector * Main.rand.NextFloat(1f, 3f);
                    dust = Dust.NewDustPerfect(position, dustType, velocity, Main.rand.Next(50, 100), Color.White, Main.rand.NextFloat(0.2f, 0.3f));
                    dust.customData = new SmokeDust.CustomData(new Color(255, 140, 20), true, new Color(25, 25, 25), false);
                }

                initialized = true;
            }

            Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * EasingFunctions.InExpo(1f - TimeLeftProgress) * 0.4f);

            for (int k = 0; k < 3; k++)
            {
                var angle = Main.rand.NextFloat(MathHelper.TwoPi);
                var radius = MaxRadius * EasingFunctions.OutExpo(TimeLeftProgress);
                var vector = Vector2.UnitX.RotatedBy(angle);
                var position = Projectile.Center + vector * radius * 0.9f;

                Dust.NewDustPerfect(position, ModContent.DustType<CircleGlowDust>(), vector, 0, new Color(200, 65, 0), Main.rand.NextFloat(0.7f, 1.0f));
            }

            for (int k = 0; k < 4; k++)
            {
                var angle = Main.rand.NextFloat(MathHelper.TwoPi);
                var radius = MaxRadius * EasingFunctions.OutExpo(TimeLeftProgress);
                var vector = Vector2.UnitX.RotatedBy(angle);
                var position = Projectile.Center + vector * radius * 0.9f;

                var dust = Dust.NewDustPerfect(position, DustID.Torch, vector, 0, default, Main.rand.NextFloat(1.2f, 2.0f));
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            var projCenter = projHitbox.Center.ToVector2();
            var vectorToTarget = Vector2.Normalize(targetHitbox.Center.ToVector2() - projCenter);
            var radius = MaxRadius * EasingFunctions.OutExpo(TimeLeftProgress);

            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), projCenter, projCenter + vectorToTarget * radius);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = MathF.Sign((target.Center - Projectile.Center).X);
            modifiers.SourceDamage += 2f;
            modifiers.Knockback += 2f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, Main.rand.Next(60, 60 * 4));
            Main.player[Projectile.owner].Counterweight(target.Center, Projectile.damage, Projectile.knockBack);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            ringRenderer ??= new RingRenderer(20, 16f * 3f, 16f * 3f);

            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.OverProjectiles, () =>
            {
                var thickness = MathHelper.Clamp(1f - TimeLeftProgress, 0f, 1f) * 16f * 5f;
                var radius = MaxRadius * EasingFunctions.OutExpo(TimeLeftProgress) - thickness * TimeLeftProgress * 0.5f;

                ringRenderer?
                    .SetThickness(thickness)
                    .SetRadius(radius)
                    .SetPosition(Projectile.Center + Projectile.gfxOffY * Vector2.UnitY)
                    .Draw(ModAssets.RequestEffect("CascadeExplosionRing").Prepare(parameters =>
                    {
                        parameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.MiscPath + "FireStrip", AssetRequestMode.ImmediateLoad).Value);
                        parameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.TransformWithScreenOffset);
                        parameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.05f);
                        parameters["UvRepeat"].SetValue(3f);
                        parameters["Color0"].SetValue(new Color(255, 180, 100).ToVector4());
                        parameters["Color1"].SetValue(new Color(255, 80, 0).ToVector4());
                    }));
            });

            return false;
        }
    }
}