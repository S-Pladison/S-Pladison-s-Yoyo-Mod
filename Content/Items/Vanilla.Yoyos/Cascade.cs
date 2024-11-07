using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Core.Graphics;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class CascadeAssets : ILoadable
    {
        public const string InvisiblePath = $"{_assetPath}Invisible";
        public const string StringPath = $"{_assetPath}FishingLine_WithShadow";

        public static Asset<Texture2D> GlowTexture { get; private set; } = ModContent.Request<Texture2D>($"{_assetPath}YoyoGlow_WithShadow");
        public static Asset<Texture2D> FlameTexture { get; private set; } = ModContent.Request<Texture2D>($"{_yoyoPath}Cascade_Flame");
        public static Asset<Effect> TrailEffect { get; private set; } = ModContent.Request<Effect>($"{_yoyoPath}CascadeEffect_Trail");
        public static Asset<Effect> RingEffect { get; private set; } = ModContent.Request<Effect>($"{_yoyoPath}CascadeEffect_Ring");
        public static SoundStyle StartChargingSound { get; private set; } = new($"{_yoyoPath}CascadeSound_StartCharging");

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Cascade/";

        void ILoadable.Unload()
        {
            GlowTexture = null;
            FlameTexture = null;
            TrailEffect = null;
            RingEffect = null;
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class CascadeItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Cascade;
    }

    public sealed class CascadeProjectile : VanillaYoyoBaseProjectile, IInitializableProjectile
    {
        public static readonly Color GlowColor = new(255, 180, 95);
        public static readonly int TrailPointCount = 10;

        private YoyoStringRenderer _stringRenderer;
        private StripRenderer _trailRenderer;
        private LinkedList<Vector2> _oldPositions;

        public override int ProjType => ProjectileID.Cascade;
        public override bool InstancePerEntity => true;

        void IInitializableProjectile.Initialize(Projectile _)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _stringRenderer = new YoyoStringRenderer(new IDrawYoyoStringSegments.Gradient(
               ModContent.Request<Texture2D>(ValorAssets.StringPath, AssetRequestMode.ImmediateLoad).Value,
               (Color.Transparent, true), (Color.Transparent, true), (GlowColor, true)
            ));

            _trailRenderer = new StripRenderer(Main.graphics.GraphicsDevice, capacity: TrailPointCount)
            {
                StartWidth = 35,
                EndWidth = 25
            };

            _oldPositions = [];
        }

        public override void OnKill(Projectile proj, int timeLeft)
        {
            _trailRenderer?.Dispose();
        }

        public override void AI(Projectile proj)
        {
            if (_trailRenderer is not null)
            {
                _oldPositions.AddFirst(proj.Center + proj.velocity);

                while (_oldPositions.Count > TrailPointCount)
                    _oldPositions.RemoveLast();

                _trailRenderer.SetPoints(_oldPositions);
            }

            Lighting.AddLight(proj.Center, GlowColor.ToVector3() * 0.2f);
        }

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.NewProjectile(proj.GetSource_FromAI(), proj.Center, Vector2.Zero, ModContent.ProjectileType<CascadeExplosionProjectile>(), proj.damage, proj.knockBack, proj.owner);
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            if (_trailRenderer is not null)
            {
                CascadeAssets.TrailEffect
                    .Prepare(parameters =>
                    {
                        parameters["Texture0"].SetValue(CascadeAssets.FlameTexture.Value);
                        parameters["TransformMatrix"].SetValue(GameMatrices.World * GameMatrices.Transform * GameMatrices.Projection);
                        parameters["Color0"].SetValue(new Color(255, 255, 105).ToVector4());
                        parameters["Color1"].SetValue(new Color(255, 80, 0).ToVector4());
                        parameters["Color2"].SetValue(new Color(250, 0, 50).ToVector4());
                        parameters["Color3"].SetValue(new Color(145, 25, 85).ToVector4());
                        parameters["Repeats"].SetValue(_trailRenderer.Points.Distance() / CascadeAssets.FlameTexture.Width() / 128.0f / 3.0f);
                        parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                    })
                    .Apply();

                _trailRenderer.Render();

                // Исправление отрисовки руки
                Main.spriteBatch.End(out var spriteBatchSnapshot);
                Main.spriteBatch.Begin(spriteBatchSnapshot);
            }

            var glowPosition = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var glowTexture = CascadeAssets.GlowTexture.Value;
            var glowOrigin = glowTexture.Size() * 0.5f;
            var glowScale = proj.scale * 1.2f;

            Main.spriteBatch.Draw(glowTexture, glowPosition, null, GlowColor, proj.rotation, glowOrigin, glowScale, SpriteEffects.None, 0f);

            return true;
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
    }

    public sealed class CascadeExplosionProjectile : ModProjectile, IInitializableProjectile
    {
        public static readonly int ExplosionRadius = TileUtils.TileSizeInPixels * 6;
        public static readonly int InitTimeLeft = ModUtils.SecondsToTicks(0.33f);

        private RingRenderer _ringRenderer;

        public override string Texture => CascadeAssets.InvisiblePath;
        public float LifeTimeRatio => 1f - Projectile.timeLeft / (float)InitTimeLeft;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = ExplosionRadius * 2;
            Projectile.height = ExplosionRadius * 2;

            Projectile.timeLeft = InitTimeLeft;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        void IInitializableProjectile.Initialize(Projectile proj)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _ringRenderer = new RingRenderer(Main.graphics.GraphicsDevice, 20);

            ScreenEffectManager.Punch(new ScreenEffectManager.PunchSettings() with
            {
                Position = Projectile.Center,
                Direction = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)),
                Strength = 7f,
                VibrationCyclesPerSecond = 6f,
                Frames = 15,
                DistanceFalloff = 16f * 25f
            });
        }

        public override void OnKill(int timeLeft)
        {
            _ringRenderer?.Dispose();
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * EasingFunctions.InExpo(1f - LifeTimeRatio) * 0.4f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            var projCenter = projHitbox.Center.ToVector2();
            var vectorToTarget = Vector2.Normalize(targetHitbox.Center.ToVector2() - projCenter);
            var radius = ExplosionRadius * EasingFunctions.OutExpo(LifeTimeRatio);

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
            target.AddBuff(BuffID.OnFire, Main.rand.Next(ModUtils.SecondsToTicks(1f), ModUtils.SecondsToTicks(4f)));

            Projectile.GetOwner().Counterweight(target.Center, Projectile.damage, Projectile.knockBack);
        }

        public override void PostDraw(Color lightColor)
        {
            if (_ringRenderer is null)
                return;

            var thickness = (1f - LifeTimeRatio) * TileUtils.TileSizeInPixels * 5f;
            var radius = ExplosionRadius * EasingFunctions.OutExpo(LifeTimeRatio);

            CascadeAssets.RingEffect
                .Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(CascadeAssets.FlameTexture.Value);
                    parameters["TransformMatrix"].SetValue(GameMatrices.Transform * GameMatrices.Projection);
                    parameters["Color0"].SetValue(Color.Lerp(new Color(255, 255, 105), new Color(250, 0, 50), LifeTimeRatio).ToVector4());
                    parameters["Color1"].SetValue(Color.Lerp(new Color(250, 135, 0), new Color(145, 25, 85), LifeTimeRatio).ToVector4());
                    parameters["Repeats"].SetValue(3.0f);
                    parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                })
                .Apply();

            _ringRenderer
                .SetThickness(thickness)
                .SetPointCount((int)MathHelper.Lerp(15, 20, LifeTimeRatio))
                .SetRadius(radius)
                .SetPosition(Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition);

            _ringRenderer.Render();
        }
    }
}