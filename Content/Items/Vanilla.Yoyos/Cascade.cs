using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Content.Particles;
using SPYoyoMod.Core.Graphics;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class CascadeAssets : ILoadable
    {
        public const string InvisiblePath = $"{_assetPath}Invisible";
        public const string StringPath = $"{_assetPath}FishingLine_WithShadow";

        public static Asset<Texture2D> GlowTexture { get; private set; } = ModContent.Request<Texture2D>($"{_assetPath}YoyoGlow_WithShadow");
        public static Asset<Effect> TrailEffect { get; private set; } = ModContent.Request<Effect>($"{_yoyoPath}CascadeEffect_Trail");
        public static SoundStyle StartChargingSound { get; private set; } = new($"{_yoyoPath}CascadeSound_StartCharging");

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Cascade/";

        void ILoadable.Unload()
        {
            GlowTexture = null;
            TrailEffect = null;
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
        public static readonly int TrailPointCount = 12;

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
                StartWidth = 30,
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

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            if (_trailRenderer is not null)
            {
                CascadeAssets.TrailEffect
                    .Prepare(parameters =>
                    {
                        parameters["Texture0"].SetValue(TextureAssets.MagicPixel.Value);
                        parameters["TransformMatrix"].SetValue(GameMatrices.World * GameMatrices.Transform * GameMatrices.Projection);
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

    public sealed class CascadeExplosionProjectile : ModProjectile, IInitializableProjectile, IPostDrawPixelatedProjectile
    {
        public static readonly int ExplosionRadius = TileUtils.TileSizeInPixels * 6;
        public static readonly int InitTimeLeft = ModUtils.SecondsToTicks(0.33f);

        private RingRenderer _ringRenderer;

        public override string Texture => CascadeAssets.InvisiblePath;
        public float TimeLeftProgress => 1f - Projectile.timeLeft / (float)InitTimeLeft;

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

        public void Initialize(Projectile proj)
        {
            for (int i = 0; i < 15; i++)
            {
                /*var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                var position = Projectile.Center + vector * Main.rand.NextFloat(MaxRadius * 0.75f);
                var velocity = vector * Main.rand.NextFloat(1f, 3f);
                var dust = Dust.NewDustPerfect(position, dustType, velocity, Main.rand.Next(50, 100), Color.White, Main.rand.NextFloat(0.2f, 0.3f));
                dust.customData = new SmokeDust.CustomData(new Color(255, 140, 20), true, new Color(50, 50, 50), false);

                vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                position = Projectile.Center + vector * Main.rand.NextFloat(MaxRadius * 0.75f);
                velocity = vector * Main.rand.NextFloat(1f, 3f);
                dust = Dust.NewDustPerfect(position, dustType, velocity, Main.rand.Next(50, 100), Color.White, Main.rand.NextFloat(0.2f, 0.3f));
                dust.customData = new SmokeDust.CustomData(new Color(255, 140, 20), true, new Color(25, 25, 25), false);*/

                //ParticleSystem.NewParticle<CircleGlowParticleRenderer>(new Particle(Projectile.Center, 0f));
            }

            if (Main.dedServ)
                return;

            _ringRenderer = new RingRenderer(Main.graphics.GraphicsDevice);
        }

        public override void OnKill(int timeLeft)
        {
            _ringRenderer?.Dispose();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            var projCenter = projHitbox.Center.ToVector2();
            var vectorToTarget = Vector2.Normalize(targetHitbox.Center.ToVector2() - projCenter);
            var radius = ExplosionRadius * EasingFunctions.OutExpo(TimeLeftProgress);

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

        public void PostDrawPixelated(Projectile proj)
        {
            var thickness = MathHelper.Clamp(1f - TimeLeftProgress, 0f, 1f) * TileUtils.TileSizeInPixels * 5f;
            var radius = ExplosionRadius * EasingFunctions.OutExpo(TimeLeftProgress) - thickness * TimeLeftProgress * 0.5f;

            _ringRenderer?
                .SetThickness(thickness)
                .SetPointCount(20) // Можно сделать ее динамической в зависимости от того же радиуса
                .SetRadius(radius)
                .SetPosition(Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition);

            /*CascadeAssets.ExplosionRingEffect
                .Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(CascadeAssets.ExplosionRingTexture.Value);
                    parameters["TransformMatrix"].SetValue(GameMatrices.Effect * GameMatrices.Projection);
                    parameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.05f);
                    parameters["UvRepeat"].SetValue(3f);
                    parameters["Color0"].SetValue(new Color(255, 180, 100).ToVector4());
                    parameters["Color1"].SetValue(new Color(255, 80, 0).ToVector4());
                })
                .Apply("CascadeExplosionRing");*/

            _ringRenderer?.Render();
        }
    }
}