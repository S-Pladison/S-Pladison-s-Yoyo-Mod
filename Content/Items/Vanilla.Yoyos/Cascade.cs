using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Content.Particles;
using SPYoyoMod.Core.Graphics;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
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
        public static Asset<Texture2D> StarTexture { get; private set; } = ModContent.Request<Texture2D>($"{_yoyoPath}Cascade_Star");
        public static Asset<Texture2D> FlameTexture { get; private set; } = ModContent.Request<Texture2D>($"{_yoyoPath}Cascade_Flame");
        public static Asset<Effect> TrailEffect { get; private set; } = ModContent.Request<Effect>($"{_yoyoPath}CascadeEffect_Trail");
        public static Asset<Effect> RingEffect { get; private set; } = ModContent.Request<Effect>($"{_yoyoPath}CascadeEffect_Ring");
        public static SoundStyle StartChargingSound { get; private set; } = new($"{_yoyoPath}CascadeSound_StartCharging");

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Cascade/";

        void ILoadable.Unload()
        {
            GlowTexture = null;
            StarTexture = null;
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

    public sealed class CascadeProjectile : VanillaYoyoBaseProjectile, IInitializableProjectile, IPostDrawPixelatedProjectile, IEmitLightEntity
    {
        public static readonly int TimeToStartCharging = GeneralUtils.SecondsToTicks(2f);
        public static readonly int TimeToCharge = GeneralUtils.SecondsToTicks(0.7f);
        public static readonly int AddTimeForHit = GeneralUtils.SecondsToTicks(0.2f);
        public static readonly Color GlowColor = new(255, 180, 95);
        public static readonly int TrailPointCount = 10;

        private int _aiTimer;
        private bool _charging;
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
               ModContent.Request<Texture2D>(CascadeAssets.StringPath, AssetRequestMode.ImmediateLoad).Value,
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
            UpdateVisual(proj);

            // Если йо-йо возвращается к игроку, прекращаем обработку всей логики
            if (proj.ai[0] == -1)
            {
                _aiTimer = Math.Max(_aiTimer - 2, 0);
                return;
            }

            _aiTimer++;

            switch (_charging)
            {
                case false:
                    {
                        if (_aiTimer < TimeToStartCharging)
                            break;

                        SoundEngine.PlaySound(CascadeAssets.StartChargingSound, proj.Center);

                        _aiTimer = 0;
                        _charging = true;
                    }
                    break;
                case true:
                    {
                        if (_aiTimer < TimeToCharge)
                            break;

                        if (proj.IsLocalPlayerAsOwner())
                            Projectile.NewProjectile(proj.GetSource_FromAI(), proj.Center, Vector2.Zero, ModContent.ProjectileType<CascadeExplosionProjectile>(), proj.damage, proj.knockBack, proj.owner);

                        _aiTimer = 0;
                        _charging = false;
                    }
                    break;
            }
        }

        private void UpdateVisual(Projectile proj)
        {
            if (_trailRenderer is not null)
            {
                _oldPositions.AddFirst(proj.Center + proj.velocity);

                while (_oldPositions.Count > TrailPointCount)
                    _oldPositions.RemoveLast();

                _trailRenderer.SetPoints(_oldPositions);
            }

            if (proj.velocity.Length() >= 3f && Main.rand.NextBool(4))
            {
                var particle = WorldParticleManager.SpawnParticle<LightPointParticle>();

                particle.LifeTime = GeneralUtils.SecondsToTicks(0.5f);
                particle.Position = proj.Center + Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * proj.width * Main.rand.NextFloat();
                particle.StartColor = new Color(255, 135, 90);
                particle.EndColor = new Color(255, 135, 90);
                particle.Scale = Main.rand.NextFloat(0.35f, 0.5f);
            }
        }

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (_charging)
                return;

            _aiTimer += AddTimeForHit;
        }

        public override void SendExtraAI(Projectile proj, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(_charging);
            binaryWriter.Write((ushort)_aiTimer);
        }

        public override void ReceiveExtraAI(Projectile proj, BitReader bitReader, BinaryReader binaryReader)
        {
            _charging = bitReader.ReadBit();
            _aiTimer = binaryReader.ReadUInt16();
        }

        void IEmitLightEntity.EmitLight(Entity proj)
        {
            Lighting.AddLight(proj.Center, GlowColor.ToVector3() * 0.2f);
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
                if (proj.GetOwner().heldProj == proj.whoAmI)
                {
                    Main.spriteBatch.End(out var spriteBatchSnapshot);
                    Main.spriteBatch.Begin(spriteBatchSnapshot);
                }
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

        public override void PostDraw(Projectile proj, Color lightColor)
        {
            if (!_charging)
                return;

            var chargeProgress = _aiTimer / (float)TimeToCharge;
            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;

            var glowTexture = CascadeAssets.GlowTexture.Value;
            var glowOrigin = glowTexture.Size() * 0.5f;
            var glowColor = Color.White with { A = 0 } * chargeProgress;

            Main.spriteBatch.Draw(glowTexture, position, null, glowColor, proj.rotation, glowOrigin, proj.scale, SpriteEffects.None, 0f);
        }

        void IPostDrawPixelatedProjectile.PostDrawPixelated(Projectile proj)
        {
            if (!_charging)
                return;

            var chargeProgress = _aiTimer / (float)TimeToCharge;
            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;

            var starTexture = CascadeAssets.StarTexture.Value;
            var starOrigin = starTexture.Size() * 0.5f;
            var starColor = Color.White with { A = 0 } * EasingFunctions.InOutQuad(chargeProgress);
            var starRotation = EasingFunctions.InOutQuad(chargeProgress) * MathHelper.PiOver2;
            var starScale = EasingFunctions.InOutQuad(1f - chargeProgress) * proj.scale * 2.5f;

            Main.spriteBatch.Draw(starTexture, position, null, starColor, starRotation, starOrigin, starScale, SpriteEffects.None, 0f);
        }
    }

    public sealed class CascadeExplosionProjectile : ModProjectile, IInitializableProjectile, IEmitLightEntity
    {
        public static readonly int ExplosionRadius = TileUtils.TileSizeInPixels * 6;
        public static readonly int InitTimeLeft = GeneralUtils.SecondsToTicks(0.33f);

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

            for (int i = 0; i < 25; i++)
            {
                var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                var particle = WorldParticleManager.SpawnParticle<SmokeParticle>(WorldParticleFlags.Pixelated | WorldParticleFlags.Behind);

                particle.LifeTime = GeneralUtils.SecondsToTicks(1.5f);
                particle.Position = Projectile.Center + vector * Main.rand.NextFloat(TileUtils.TileSizeInPixels, ExplosionRadius * 0.85f);
                particle.Velocity = vector * Main.rand.NextFloat(0.2f, 2f);
                particle.StartColor = new(new Color(50, 50, 50, 255), false);
                particle.EndColor = new(new Color(0, 0, 0, 0), false);
                particle.Scale = 3f;
            }

            ScreenEffectManager.Punch(new ScreenEffectManager.PunchSettings()
            {
                Position = Projectile.Center,
                Direction = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)),
                Strength = 7f,
                VibrationCyclesPerSecond = 6f,
                Frames = 15,
                DistanceFalloff = 16f * 25f
            });

            SoundEngine.PlaySound(SoundID.Item14, proj.Center);
        }

        public override void OnKill(int timeLeft)
        {
            _ringRenderer?.Dispose();
        }

        public override void AI()
        {
            var radius = ExplosionRadius * EasingFunctions.OutExpo(LifeTimeRatio);
            var quantity = 5 * (radius / ExplosionRadius);

            for (int k = 0; k < quantity; k++)
            {
                var angle = Main.rand.NextFloat(MathHelper.TwoPi);
                var particle = WorldParticleManager.SpawnParticle<LightPointParticle>();

                particle.LifeTime = GeneralUtils.SecondsToTicks(0.5f);
                particle.Position = Projectile.Center + Vector2.UnitX.RotatedBy(angle) * radius * 0.95f;
                particle.Velocity = Vector2.UnitX.RotatedBy(angle) * 0.5f;
                particle.StartColor = new Color(255, 135, 90);
                particle.EndColor = new Color(255, 135, 90);
                particle.Scale = Main.rand.NextFloat(0.35f, 0.5f);
            }

            for (int k = 0; k < quantity; k++)
            {
                var angle = Main.rand.NextFloat(MathHelper.TwoPi);
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
            target.AddBuff(BuffID.OnFire, Main.rand.Next(GeneralUtils.SecondsToTicks(1f), GeneralUtils.SecondsToTicks(4f)));

            Projectile.GetOwner().Counterweight(target.Center, Projectile.damage, Projectile.knockBack);
        }

        void IEmitLightEntity.EmitLight(Entity _)
        {
            Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * EasingFunctions.InExpo(1f - LifeTimeRatio) * 0.4f);
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