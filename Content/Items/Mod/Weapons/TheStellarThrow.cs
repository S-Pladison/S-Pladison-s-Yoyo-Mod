﻿using Microsoft.Xna.Framework;
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
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class TheStellarThrowItem : YoyoItem
    {
        public const int LootChanceDenominator = 3;

        public override string Texture => ModAssets.ItemsPath + "TheStellarThrow";
        public override int GamepadExtraRange => 9;

        public override void YoyoSetDefaults()
        {
            Item.damage = 18;
            Item.knockBack = 3f;

            Item.shoot = ModContent.ProjectileType<TheStellarThrowProjectile>();

            Item.rare = ItemRarityID.Green;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 0, copper: 0);
        }
    }

    public class TheStellarThrowProjectile : YoyoProjectile
    {
        public const float SpawnStarRadius = 16f * 15f;
        public const float SpawnStarCooldownMin = 60f * 1.5f;
        public const float SpawnStarCooldownMax = 60f * 2f;

        private TrailRenderer trailRenderer;

        public override string Texture => ModAssets.ProjectilesPath + "TheStellarThrow";
        public override float LifeTime => 8f;
        public override float MaxRange => 215f;
        public override float TopSpeed => 13f;
        public ref float StarTimer => ref Projectile.ai[2];

        public override void YoyoOnSpawn(Player owner, IEntitySource source)
        {
            StarTimer = Main.rand.NextFloat(SpawnStarCooldownMin, SpawnStarCooldownMax);
        }

        public override void AI()
        {
            if (Projectile.owner == Main.myPlayer)
            {
                StarTimer--;

                if (StarTimer <= 0)
                {
                    var targets = EntityUtils.NearestNPCs(Projectile.Center, SpawnStarRadius, (npc) => npc.CanBeChasedBy(Projectile, false));

                    if (targets.Count > 0)
                    {
                        var npc = targets[Main.rand.Next(targets.Count)].npc;
                        var starPosition = npc.Center - Vector2.UnitY * 16f * 50f + Vector2.UnitX * (Main.rand.NextBool() ? 1 : -1) * 16f * Main.rand.NextFloat(20f, 60f);
                        var starVelosity = Vector2.Normalize(npc.Center - starPosition) * 24f;

                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), starPosition, starVelosity, ModContent.ProjectileType<TheStellarThrowStarProjectile>(), Projectile.damage, Projectile.knockBack, Projectile.owner, npc.whoAmI);

                        StarTimer = Main.rand.NextFloat(SpawnStarCooldownMin, SpawnStarCooldownMax);
                    }
                }
            }

            var isMoving = Projectile.velocity.Length() >= 3f;

            if (isMoving && Main.rand.NextBool(2))
            {
                var isStarDust = Main.rand.NextBool();
                var dustType = isStarDust ? ModContent.DustType<StarGlowDust>() : ModContent.DustType<CircleGlowDust>();
                var dustColor = Color.Lerp(new Color(255, 175, 65), new Color(255, 85, 191), Main.rand.NextFloat(0, 1));
                var dustScale = isStarDust ? Main.rand.NextFloat(1f, 3f) : Main.rand.NextFloat(0.5f, 0.8f);
                var dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 0, dustColor, dustScale);
                var dust = Main.dust[dustIndex];

                dust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                dust.velocity *= 0.4f;
            }

            trailRenderer?.SetNextPoint(Projectile.Center + Projectile.velocity);

            Lighting.AddLight(Projectile.Center, new Color(160, 30, 120).ToVector3() * 0.2f);
        }

        public override void PostDrawYoyoString(Vector2 mountedCenter)
        {
            DrawUtils.DrawGradientYoyoStringWithShadow(Projectile, mountedCenter, (Color.Transparent, true), (new Color(252, 194, 116), true));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trailRenderer ??= new TrailRenderer(15, f => MathHelper.Lerp(40f, 20f, f));

            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.UnderProjectiles, () =>
            {
                if (trailRenderer is not null)
                {
                    var length = trailRenderer.Points.DistanceBetween();

                    if (length > 0f)
                    {
                        trailRenderer.Draw(ModAssets.RequestEffect("TheStellarThrowTrail").Prepare(parameters =>
                        {
                            parameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.MiscPath + "TheStellarThrow_Trail2", AssetRequestMode.ImmediateLoad).Value);
                            parameters["Texture1"].SetValue(ModContent.Request<Texture2D>(ModAssets.MiscPath + "TheStellarThrow_Trail", AssetRequestMode.ImmediateLoad).Value);
                            parameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.TransformWithScreenOffset);
                            parameters["Color0"].SetValue(new Color(160, 30, 120).ToVector4());
                            parameters["Color1"].SetValue(new Color(255, 240, 185).ToVector4());
                            parameters["Color2"].SetValue(new Color(255, 0, 80).ToVector4());
                            parameters["UvRepeat"].SetValue(length / 500f);
                            parameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.01f);
                        }));
                    }
                }

                var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Star", AssetRequestMode.ImmediateLoad);
                var color = new Color(100, 25, 75) * 0.25f;

                Main.spriteBatch.Draw(texture.Value, position, null, color, Projectile.rotation * 0.05f, texture.Size() * 0.5f, 0.6f, SpriteEffects.None, 0f);

                color = new Color(255, 0, 80) with { A = 0 };

                Main.spriteBatch.Draw(texture.Value, position, null, color, Projectile.rotation * 0.1f, texture.Size() * 0.5f, 0.4f, SpriteEffects.None, 0f);
            });

            var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Yoyo_GlowWithShadow", AssetRequestMode.ImmediateLoad);

            Main.spriteBatch.Draw(texture.Value, position, null, Color.Black * 0.2f, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 1.5f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture.Value, position, null, new Color(252, 194, 116), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 1.2f, SpriteEffects.None, 0f);

            return true;
        }
    }

    public class TheStellarThrowStarProjectile : ModProjectile
    {
        private static readonly Tuple<Color, Color, Color>[] colors = new Tuple<Color, Color, Color>[]
        {
            new(new Color(160, 30, 120), new Color(255, 240, 185), new Color(255, 0, 80)),
            new(new Color(85, 30, 160), new Color(185, 240, 255), new Color(0, 135, 255)),
            new(new Color(165, 35, 35), new Color(255, 180, 205), new Color(105, 0, 255)),
            new(new Color(30, 110, 160), new Color(185, 255, 230), new Color(0, 255, 190))
        };

        private bool initialized;
        private float initSpeed;
        private int style;
        private float yToBecomeCollidable;
        private TrailRenderer trailRenderer;

        public override string Texture => ModAssets.ProjectilesPath + "TheStellarThrow";
        public int TargetIndex => (int)Projectile.ai[0];
        public Tuple<Color, Color, Color> StyleColors => colors[style];

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = 20;
            Projectile.height = 20;

            Projectile.timeLeft = 60 * 3;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 14; i++)
            {
                var isStarDust = Main.rand.NextBool();
                var dustType = isStarDust ? ModContent.DustType<StarGlowDust>() : ModContent.DustType<CircleGlowDust>();
                var dustColor = Color.Lerp(StyleColors.Item2, StyleColors.Item3, Main.rand.NextFloat(0, 1));
                var dustScale = isStarDust ? Main.rand.NextFloat(1f, 3f) : Main.rand.NextFloat(0.5f, 0.8f);
                var dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 0, dustColor, dustScale);
                var dust = Main.dust[dustIndex];

                dust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                dust.velocity *= 1.5f;
            }

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<TheStellarThrowHitProjectile>(), 0, 0, Projectile.owner);
        }

        public override void AI()
        {
            if (!initialized)
            {
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                Projectile.scale = 0.01f;

                initSpeed = Projectile.velocity.Length();
                style = Main.rand.Next(colors.Length);
                yToBecomeCollidable = (TargetIndex >= 0 && Main.npc[TargetIndex] is NPC target && target.active) ? (target.Top.Y + 2) : 0f;
                initialized = true;
            }

            Projectile.rotation += 0.5f;
            Projectile.scale = MathHelper.Min(1f, Projectile.scale + 0.1f);

            if (!Projectile.tileCollide && Projectile.Center.Y >= yToBecomeCollidable)
            {
                Projectile.tileCollide = true;
            }

            if (!Projectile.tileCollide && Projectile.Center.Y < yToBecomeCollidable && TargetIndex >= 0 && Main.npc[TargetIndex] is NPC npc && npc.active)
            {
                Projectile.MoveTo(npc.Center, initSpeed, 1f);
            }

            if (Projectile.velocity.Length() >= 3f && Main.rand.NextBool(2))
            {
                var isStarDust = Main.rand.NextBool();
                var dustType = isStarDust ? ModContent.DustType<StarGlowDust>() : ModContent.DustType<CircleGlowDust>();
                var dustColor = Color.Lerp(StyleColors.Item2, StyleColors.Item3, Main.rand.NextFloat(0, 1));
                var dustScale = isStarDust ? Main.rand.NextFloat(1f, 3f) : Main.rand.NextFloat(0.5f, 0.8f);
                var dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 0, dustColor, dustScale);
                var dust = Main.dust[dustIndex];

                dust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                dust.velocity *= 0.4f;
            }

            trailRenderer?.SetNextPoint(Projectile.Center + Projectile.velocity);

            if (Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = 60;

                SoundEngine.PlaySound(in SoundID.Item9, Projectile.Center);
            }

            Lighting.AddLight(Projectile.Center, StyleColors.Item1.ToVector3() * 0.3f);
        }

        public override bool? CanHitNPC(NPC target)
        {
            return target.CanBeChasedBy(Projectile, false) || target.type == NPCID.TargetDummy;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage += 1f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.tileCollide = true;
            Main.player[Projectile.owner].Counterweight(target.Center, Projectile.damage, Projectile.knockBack);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = Vector2.Zero;
            return true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trailRenderer ??= new TrailRenderer(15, f => MathHelper.Lerp(40f, 20f, f) * Projectile.scale);

            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.UnderProjectiles, () =>
            {
                if (trailRenderer is not null)
                {
                    var length = trailRenderer.Points.DistanceBetween();

                    if (length > 0f)
                    {
                        trailRenderer.Draw(ModAssets.RequestEffect("TheStellarThrowTrail").Prepare(parameters =>
                        {
                            parameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.MiscPath + "TheStellarThrow_Trail2", AssetRequestMode.ImmediateLoad).Value);
                            parameters["Texture1"].SetValue(ModContent.Request<Texture2D>(ModAssets.MiscPath + "TheStellarThrow_Trail", AssetRequestMode.ImmediateLoad).Value);
                            parameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.TransformWithScreenOffset);
                            parameters["Color0"].SetValue(StyleColors.Item1.ToVector4());
                            parameters["Color1"].SetValue(StyleColors.Item2.ToVector4());
                            parameters["Color2"].SetValue(StyleColors.Item3.ToVector4());
                            parameters["UvRepeat"].SetValue(length / 500f);
                            parameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.01f);
                        }));
                    }
                }

                var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Star", AssetRequestMode.ImmediateLoad);
                var color = StyleColors.Item1 * 0.25f;

                Main.spriteBatch.Draw(texture.Value, position, null, color, Projectile.rotation * 0.05f, texture.Size() * 0.5f, Projectile.scale * 0.6f, SpriteEffects.None, 0f);

                color = StyleColors.Item3 with { A = 0 };

                Main.spriteBatch.Draw(texture.Value, position, null, color, Projectile.rotation * 0.1f, texture.Size() * 0.5f, Projectile.scale * 0.4f, SpriteEffects.None, 0f);

                color = StyleColors.Item2 with { A = 0 };

                Main.spriteBatch.Draw(texture.Value, position, null, color, Projectile.rotation * 0.1f, texture.Size() * 0.5f, Projectile.scale * 0.35f, SpriteEffects.None, 0f);
            });

            return false;
        }
    }

    public class TheStellarThrowHitProjectile : ModProjectile
    {
        public const float InitTimeLeft = 25;

        private static readonly EasingBuilder scaleEasing = new(
            (EasingFunctions.InOutExpo, 0.2f, 0f, 1f),
            (EasingFunctions.InOutQuad, 0.8f, 1f, 0f)
        );

        public override string Texture => ModAssets.MiscPath + "Invisible";

        public override void SetDefaults()
        {
            Projectile.DefaultToVisualEffect();

            Projectile.timeLeft = (int)InitTimeLeft;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI()
        {
            Projectile.rotation += 0.3f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.UnderProjectiles, () =>
            {
                var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Star", AssetRequestMode.ImmediateLoad);
                var color = new Color(100, 25, 75) * 0.25f;
                var scale = scaleEasing.Evaluate(1f - Projectile.timeLeft / InitTimeLeft);

                Main.spriteBatch.Draw(texture.Value, position, null, color, Projectile.rotation * 0.05f, texture.Size() * 0.5f, scale * 0.8f, SpriteEffects.None, 0f);

                color = new Color(255, 0, 80) with { A = 0 };

                Main.spriteBatch.Draw(texture.Value, position, null, color, Projectile.rotation * 0.1f, texture.Size() * 0.5f, scale * 0.6f, SpriteEffects.None, 0f);
            });

            return false;
        }
    }
}