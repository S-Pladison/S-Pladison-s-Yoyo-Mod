using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Yoyos;
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
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class CascadeAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Cascade/Cascade";

        public const string InvisiblePath = $"{AssetPath}/Invisible";
        public const string StringPath = $"{AssetPath}/FishingLine_WithShadow";

        public static readonly LazyAsset<Texture2D> GlowTexture = LazyAsset<Texture2D>.From($"{AssetPath}/YoyoGlow_WithShadow");
        public static readonly LazyAsset<Texture2D> StarTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Star");
        public static readonly LazyAsset<Texture2D> FlameTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Flame");
        public static readonly LazyAsset<Texture2D> NoiseTexture = LazyAsset<Texture2D>.From($"{AssetPath}/WaveNoise");
        public static readonly LazyAsset<Effect> TrailEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Trail");
        public static readonly LazyAsset<Effect> SphereEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Sphere");
        public static readonly SoundStyle StartChargingSound = new($"{YoyoPath}Sound_StartCharging");
        public static readonly SoundStyle ExplosionSound = SoundID.Item14;
    }

    public sealed class CascadeItem : YoyoItem<CascadeProjectile>
    {
        public override int OverrideType => ItemID.Cascade;
    }

    public sealed class CascadeProjectile : YoyoProjectile<CascadeItem>, IInitializableProjectile, IEmitLightEntity, IPostDrawPixelatedProjectile
    {
        public override int OverrideType => ProjectileID.Cascade;
        public override bool DisableVanillaSpecials => true;
        public override float? LifeTime => -1f;

        //=/-

        public static readonly int TimeToStartCharging = GeneralUtils.SecondsToTicks(2f);
        public static readonly int TimeToCharge = GeneralUtils.SecondsToTicks(0.7f);
        public static readonly int HitsPerMiniExplosion = 3;
        public static readonly Color GlowColor = new(255, 180, 95);
        public static readonly int TrailPointCount = 10;

        private int _aiTimer;
        private int _hitCount;
        private bool _charging;
        private YoyoStringRenderer _stringRenderer;
        private StripRenderer _trailRenderer;
        private LinkedList<Vector2> _oldPositions;

        void IInitializableProjectile.Initialize(Projectile proj)
        {
            if (Main.dedServ)
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
            if (IsReturning)
            {
                _aiTimer = Math.Max(_aiTimer - 2, 0);
                return;
            }

            if (!proj.IsPrimaryYoyo())
                return;

            _aiTimer++;

            switch (_charging)
            {
                case false:
                    {
                        if (_aiTimer < TimeToStartCharging)
                            break;

                        SoundEngine.PlaySound(CascadeAssets.StartChargingSound, proj.Center);

                        _aiTimer = 0;
                        _hitCount = 0;
                        _charging = true;
                    }
                    break;
                case true:
                    {
                        if (_aiTimer < TimeToCharge)
                            break;

                        SpawnExplosion(proj, proj.Center, 1f, proj.GetSource_FromAI());

                        _aiTimer = 0;
                        _hitCount = 0;
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
            if (Main.rand.NextBool(3))
                target.AddBuff(BuffID.OnFire, Main.rand.Next(GeneralUtils.SecondsToTicks(1f), GeneralUtils.SecondsToTicks(4f)));

            if (_charging)
                return;

            _hitCount++;

            if (_hitCount < HitsPerMiniExplosion)
                return;

            _hitCount = 0;

            SpawnExplosion(proj, target.Center, 0.45f, proj.GetSource_OnHit(target));
        }

        private static void SpawnExplosion(Projectile proj, Vector2 position, float scale, IEntitySource source)
        {
            if (!proj.IsLocalPlayerAsOwner())
                return;

            Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<CascadeExplosionProjectile>(), proj.damage, proj.knockBack, proj.owner, scale);
        }

        public override void SendExtraAI(Projectile proj, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(_charging);
            binaryWriter.Write((ushort)_aiTimer);
        }

        public override void ReceiveExtraAI(Projectile proj, BitReader bitReader, BinaryReader binaryReader)
        {
            _charging = binaryReader.ReadBoolean();
            _aiTimer = binaryReader.ReadUInt16();
        }

        void IEmitLightEntity.EmitLight(Entity entity)
        {
            Lighting.AddLight(entity.Center, GlowColor.ToVector3() * 0.2f);
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
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
                    parameters["Repeats"].SetValue(_trailRenderer.Points.Distance() / CascadeAssets.FlameTexture.Value.Width / 128.0f / 3.0f);
                    parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                })
                .Apply();

            _trailRenderer.Render();

            // Исправление отрисовки руки
            if (proj.TryGetOwner(out var owner) && owner.heldProj == proj.whoAmI)
            {
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
            _stringRenderer.Render(Main.spriteBatch, YoyoStringRendererContext.FromProjectile(proj, mountedCenter));
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

    public sealed class CascadeExplosionProjectile : ModProjectile, IInitializableProjectile, IEmitLightEntity, IPostDrawPixelatedProjectile
    {
        public static readonly int MaxExplosionRadius = TileUtils.TileSizeInPixels * 6;
        public static readonly int MaxRingThickness = TileUtils.TileSizeInPixels * 5;
        public static readonly int InitTimeLeft = GeneralUtils.SecondsToTicks(0.33f);

        private RectangleRenderer _sphereRenderer;

        public override string Texture => CascadeAssets.InvisiblePath;

        public float LifeTimeRatio => 1f - Projectile.timeLeft / (float)InitTimeLeft;
        public int MaxRadius => Math.Max((int)(MaxExplosionRadius * Scale), 1);
        public float Radius => MaxRadius * EasingFunctions.OutExpo(LifeTimeRatio);
        public float Scale => Projectile.ai[0] > 0f ? Projectile.ai[0] : 1f;
        public float RingThickness => (1f - LifeTimeRatio) * MaxRingThickness * Scale;
        public float SphereRadius => Math.Max(Radius - RingThickness * 0.55f, 0f);

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = MaxExplosionRadius * 2;
            Projectile.height = MaxExplosionRadius * 2;

            Projectile.timeLeft = InitTimeLeft;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.noEnchantmentVisuals = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        void IInitializableProjectile.Initialize(Projectile proj)
        {
            if (Main.dedServ)
                return;

            _sphereRenderer = new RectangleRenderer(Main.graphics.GraphicsDevice);

            for (int i = 0; i < Math.Max((int)(25 * Scale * Scale), 1); i++)
            {
                var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                var particle = WorldParticleManager.SpawnParticle<SmokeParticle>(WorldParticleFlags.Pixelated | WorldParticleFlags.Behind);

                particle.LifeTime = GeneralUtils.SecondsToTicks(1.5f);
                particle.Position = proj.Center + vector * Main.rand.NextFloat(TileUtils.TileSizeInPixels * Scale, MaxRadius * 0.85f);
                particle.Velocity = vector * Main.rand.NextFloat(0.2f, 2f) * Scale;
                particle.StartColor = new(new Color(50, 50, 50, 255), false);
                particle.EndColor = new(new Color(0, 0, 0, 0), false);
                particle.Scale = 3f * Scale;
            }

            if (Scale >= 0.5f)
            {
                ScreenEffectManager.Punch(new ScreenEffectManager.PunchSettings()
                {
                    Position = proj.Center,
                    Direction = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)),
                    Strength = 7f * Scale,
                    VibrationCyclesPerSecond = 6f,
                    Frames = Math.Max((int)(15 * Scale), 8),
                    DistanceFalloff = 16f * 25f * Scale
                });
            }

            SoundEngine.PlaySound(CascadeAssets.ExplosionSound with { Volume = Scale }, proj.Center);
        }

        public override void OnKill(int timeLeft)
        {
            _sphereRenderer?.Dispose();
        }

        public override void AI()
        {
            var count = 5 * (Radius / MaxRadius) * Scale;

            for (int k = 0; k < count; k++)
            {
                var angle = Main.rand.NextFloat(MathHelper.TwoPi);
                var particle = WorldParticleManager.SpawnParticle<LightPointParticle>();

                particle.LifeTime = GeneralUtils.SecondsToTicks(0.5f);
                particle.Position = Projectile.Center + Vector2.UnitX.RotatedBy(angle) * Radius * 0.95f;
                particle.Velocity = Vector2.UnitX.RotatedBy(angle) * 0.5f;
                particle.StartColor = new Color(255, 135, 90);
                particle.EndColor = new Color(255, 135, 90);
                particle.Scale = Main.rand.NextFloat(0.35f, 0.5f);
            }

            for (int k = 0; k < count; k++)
            {
                var angle = Main.rand.NextFloat(MathHelper.TwoPi);
                var vector = Vector2.UnitX.RotatedBy(angle);
                var position = Projectile.Center + vector * Radius * 0.9f;
                var dust = Dust.NewDustPerfect(position, DustID.Torch, vector, 0, default, Main.rand.NextFloat(1.2f, 2.0f));

                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CollisionUtils.CheckRectanglevCircle(targetHitbox, Projectile.Center, Radius);

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = MathF.Sign((target.Center - Projectile.Center).X);
            modifiers.SourceDamage += 2f * Scale;
            modifiers.Knockback += 2f * Scale;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, Main.rand.Next(GeneralUtils.SecondsToTicks(1f), GeneralUtils.SecondsToTicks(4f)));

            if (Projectile.TryGetOwner(out var owner))
                owner.Counterweight(target.Center, Projectile.damage, Projectile.knockBack);
        }

        void IEmitLightEntity.EmitLight(Entity _)
        {
            Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * EasingFunctions.InExpo(1f - LifeTimeRatio) * 0.4f * Scale);
        }

        void IPostDrawPixelatedProjectile.PostDrawPixelated(Projectile proj)
        {
            var outer = Math.Max(Radius, Math.Abs(Radius - RingThickness));
            var quadHalf = Math.Max(outer, 1f);

            CascadeAssets.SphereEffect
                .Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(CascadeAssets.NoiseTexture.Value);
                    parameters["Texture1"].SetValue(CascadeAssets.FlameTexture.Value);
                    parameters["TransformMatrix"].SetValue(GameMatrices.World * GameMatrices.Effect * GameMatrices.Projection);
                    parameters["Color0"].SetValue(Color.Lerp(new(255, 255, 105), new(250, 0, 50), LifeTimeRatio).ToVector4());
                    parameters["Color1"].SetValue(Color.Lerp(new(250, 135, 0), new(145, 25, 85), LifeTimeRatio).ToVector4());
                    parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                    parameters["Repeats"].SetValue(6);
                    parameters["SphereRatio"].SetValue(SphereRadius / quadHalf);
                    parameters["RingInner"].SetValue(Math.Max(Radius - RingThickness, 0f) / quadHalf);
                    parameters["RingOuter"].SetValue(outer / quadHalf);
                })
                .Apply();

            _sphereRenderer
                .SetColor(Color.White * (1f - EasingFunctions.InQuad(LifeTimeRatio)))
                .SetSize(quadHalf * 2f)
                .SetPosition(proj.Center + proj.gfxOffY * Vector2.UnitY)
                .Render();
        }
    }
}