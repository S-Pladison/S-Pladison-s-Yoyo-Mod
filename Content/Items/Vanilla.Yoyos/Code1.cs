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
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class Code1Assets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Code1/Code1";

        public const string InvisiblePath = $"{AssetPath}/Invisible";

        public static readonly LazyAsset<Texture2D> NoiseTexture = LazyAsset<Texture2D>.From($"{AssetPath}/WaveNoise");
        public static readonly LazyAsset<Effect> ScreenEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Screen");
        public static readonly LazyAsset<Effect> SphereEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Sphere");
        public static readonly SoundStyle InfectSound = new("Terraria/Sounds/Item_182");
        public static readonly SoundStyle BurstSound = SoundID.Item77;
    }

    public sealed class Code1Item : YoyoItem<Code1Projectile>
    {
        public override int OverrideType => ItemID.Code1;
    }

    public sealed class Code1Projectile : YoyoProjectile<Code1Item>, IHaveHitEffectProjectile
    {
        public override int OverrideType => ProjectileID.Code1;

        //=/-

        public static readonly int WaveCooldown = GeneralUtils.SecondsToTicks(3f);
        public static readonly int WaveApplyChanceDenominator = 5;
        public static readonly int WaveMinRemainingHits = 3;

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsReturning || !Main.rand.NextBool(WaveApplyChanceDenominator))
                return;

            var remainingLife = target.IsChild(out var parent) ? parent.life : target.life;

            if (remainingLife <= damageDone * WaveMinRemainingHits)
                return;

            if (!proj.TryGetOwner(out var owner) || owner.IsCooldownActiveFor<Code1Projectile>())
                return;

            var waveType = ModContent.ProjectileType<Code1DigitalWaveProjectile>();

            if (owner.ownedProjectileCounts[waveType] > 0 || IsInfected(target))
                return;

            Projectile.NewProjectile(proj.GetSource_OnHit(target), target.Center, Vector2.Zero, waveType, proj.damage, 0f, proj.owner, target.whoAmI);

            owner.SetCooldownFor<Code1Projectile>(WaveCooldown);
        }

        private bool IsInfected(NPC target)
        {
            foreach (var otherProj in Main.ActiveProjectiles)
            {
                if (otherProj.type != ModContent.ProjectileType<Code1DigitalWaveProjectile>())
                    continue;

                if ((otherProj.As<Code1DigitalWaveProjectile>()?.Target ?? -1) == target.whoAmI)
                    return true;
            }

            return false;
        }

        void IHaveHitEffectProjectile.HitEffect(Projectile proj, NPC target, NPC.HitInfo hit)
        {
            if (Main.dedServ || IsInfected(target))
                return;

            var origin = Vector2.Lerp(proj.Center, target.Center, 0.5f);
            var count = 1 + Math.Max(Main.rand.Next(5) - 2, 0);

            for (var i = 0; i < count; i++)
            {
                var direction = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                var color = Main.rand.NextBool() ? Code1DigitalWaveProjectile.ChargeColor : Code1DigitalWaveProjectile.BurstColor;
                var particle = WorldParticleManager.SpawnParticle<RotatingCubeParticle>(WorldParticleFlags.Pixelated);

                particle.LifeTime = GeneralUtils.SecondsToTicks(0.8f);
                particle.Position = origin;
                particle.Velocity = direction * Main.rand.NextFloat(1.5f, 3.2f);
                particle.StartColor = color;
                particle.EndColor = color;
                particle.Scale = Main.rand.NextFloat(0.45f, 0.7f);
            }
        }
    }

    public sealed class Code1DigitalWaveProjectile : ModProjectile, IInitializableProjectile, IEmitLightEntity, IHaveHitEffectProjectile, IPostDrawPixelatedProjectile
    {
        private enum State
        {
            Charge,
            Burst
        }

        public static readonly Color ChargeColor = new(90, 175, 255);
        public static readonly Color BurstColor = new(235, 26, 42);
        public static readonly float MinChargeRadius = TileUtils.TileSizeInPixels * 3f;
        public static readonly float MaxWaveRadius = TileUtils.TileSizeInPixels * 16f;
        public static readonly int InitTimeLeft = GeneralUtils.SecondsToTicks(2f);
        public static readonly float BurstStartRatio = 0.75f;
        public static readonly int BurstDuration = InitTimeLeft - (int)(InitTimeLeft * BurstStartRatio);

        private static readonly EasingBuilder _radiusEasing = new(
            (EasingFunctions.OutCubic, 0.06f, 0f, 1f),
            (EasingFunctions.Linear, 0.59f, 1f, 1f),
            (EasingFunctions.InCubic, 0.10f, 1f, 0.12f),
            (EasingFunctions.OutExpo, 0.25f, 0.12f, MaxWaveRadius / MinChargeRadius)
        );

        private static readonly EasingBuilder _strengthEasing = new(
            (EasingFunctions.OutQuad, 0.06f, 0f, 1f),
            (EasingFunctions.Linear, 0.69f, 1f, 1f),
            (EasingFunctions.InCubic, 0.25f, 1f, 0f)
        );

        private static readonly EasingBuilder _npcOutlineEasing = new(
            (EasingFunctions.OutQuad, 0.10f, 0f, 1f),
            (EasingFunctions.Linear, 0.65f, 1f, 1f),
            (EasingFunctions.InCubic, 0.25f, 1f, 0f)
        );

        private float _chargeRadius = MinChargeRadius;
        private State _state = State.Charge;
        private RectangleRenderer _sphereRenderer;

        public override string Texture => Code1Assets.InvisiblePath;

        public int Target => (int)Projectile.ai[0];
        public bool IsBursting => _state == State.Burst;
        public float LifeTimeRatio => 1f - Projectile.timeLeft / (float)InitTimeLeft;

        public float Radius => _chargeRadius * _radiusEasing.Evaluate(LifeTimeRatio);
        public float Strength => _strengthEasing.Evaluate(LifeTimeRatio);
        public float Fill => IsBursting ? 0f : 1f;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = (int)(MaxWaveRadius * 2);
            Projectile.height = (int)(MaxWaveRadius * 2);

            Projectile.timeLeft = InitTimeLeft;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.noEnchantmentVisuals = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        void IInitializableProjectile.Initialize(Projectile _)
        {
            if (TryGetTarget(out var npc))
                RefreshChargeValueFromTarget(npc);

            if (Main.dedServ)
                return;

            _sphereRenderer = new RectangleRenderer(Main.graphics.GraphicsDevice);

            TryOutlineTarget();

            ModContent.GetInstance<Code1ScreenEffectHandler>()?.Add(Projectile);
            SoundEngine.PlaySound(Code1Assets.InfectSound with { Pitch = 0.7f, PitchVariance = 0.08f }, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            _sphereRenderer?.Dispose();

            ModContent.GetInstance<Code1ScreenEffectHandler>()?.Remove(Projectile);
        }

        public override void AI()
        {
            if (!IsBursting)
            {
                var hasTarget = TryGetTarget(out var npc);

                if (hasTarget)
                    RefreshChargeValueFromTarget(npc);

                if (!hasTarget || LifeTimeRatio >= BurstStartRatio)
                    SetState(State.Burst);
            }

            if (Main.dedServ || IsBursting)
                return;

            SpawnChargeParticle();
        }

        public override bool ShouldUpdatePosition()
            => false;

        public override bool? CanDamage()
            => IsBursting;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CollisionUtils.CheckRectanglevCircle(targetHitbox, Projectile.Center, Radius * 0.85f);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.TryGetOwner(out var owner))
                owner.Counterweight(target.Center, Projectile.damage, Projectile.knockBack);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X >= Projectile.Center.X ? 1 : -1;
            modifiers.SetCrit();
        }

        void IHaveHitEffectProjectile.HitEffect(Projectile _, NPC target, NPC.HitInfo hit)
        {
            if (Main.dedServ)
                return;

            var size = new Vector2(target.width, target.height) * 0.8f;
            var vectorToTarget = target.Center - Projectile.Center;

            for (var i = 0; i < 3 + Main.rand.Next(2); i++)
            {
                var position = target.Center + new Vector2(Main.rand.NextFloat(-0.5f, 0.5f) * size.X, Main.rand.NextFloat(-0.5f, 0.5f) * size.Y);
                var direction = vectorToTarget.LengthSquared() > 0.01f ? Vector2.Normalize(vectorToTarget).RotatedBy(Main.rand.NextFloat(-0.7f, 0.7f)) : Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                var color = Main.rand.NextBool() ? ChargeColor : BurstColor;
                var particle = WorldParticleManager.SpawnParticle<RotatingCubeParticle>(WorldParticleFlags.Pixelated);

                particle.LifeTime = GeneralUtils.SecondsToTicks(0.7f);
                particle.Position = position;
                particle.Velocity = direction * Main.rand.NextFloat(1.2f, 2.6f);
                particle.StartColor = color;
                particle.EndColor = color;
                particle.Scale = Main.rand.NextFloat(0.45f, 0.7f);
            }
        }

        public override bool? CanCutTiles()
            => false;

        public Color GetOutlineColor(in float lifeTimeRatio)
            => (IsBursting ? BurstColor : ChargeColor) * _npcOutlineEasing.Evaluate(lifeTimeRatio);

        void IEmitLightEntity.EmitLight(Entity _)
        {
            Lighting.AddLight(Projectile.Center, GetOutlineColor(LifeTimeRatio).ToVector3() * Strength * 0.35f);
        }

        public override bool PreDraw(ref Color lightColor)
            => false;

        void IPostDrawPixelatedProjectile.PostDrawPixelated(Projectile proj)
        {
            if (!IsBursting)
                return;

            var progress = Math.Clamp((BurstDuration - Projectile.timeLeft) / (float)BurstDuration, 0f, 1f);
            var opacity = 1f - EasingFunctions.InCubic(progress);

            if (opacity <= 0.01f || Radius <= 0f)
                return;

            Code1Assets.SphereEffect
                .Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(Code1Assets.NoiseTexture.Value);
                    parameters["TransformMatrix"].SetValue(GameMatrices.World * GameMatrices.Effect * GameMatrices.Projection);
                    parameters["Color0"].SetValue(BurstColor.ToVector4());
                    parameters["Color1"].SetValue(Color.Black.ToVector4());
                    parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                })
                .Apply();

            _sphereRenderer
                .SetColor(Color.White * opacity)
                .SetSize(Radius * 2f)
                .SetPosition(proj.Center + proj.gfxOffY * Vector2.UnitY)
                .Render();
        }

        private void SetState(State state)
        {
            if (_state == state)
                return;

            _state = state;

            if (state != State.Burst)
                return;

            if (Projectile.timeLeft > BurstDuration)
                Projectile.timeLeft = BurstDuration;

            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(Code1Assets.BurstSound with { Pitch = 0.55f, PitchVariance = 0.12f }, Projectile.Center);

            ScreenEffectManager.Punch(new ScreenEffectManager.PunchSettings()
            {
                Position = Projectile.Center,
                Direction = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)),
                Strength = 1.6f,
                VibrationCyclesPerSecond = 5f,
                Frames = 5,
                DistanceFalloff = TileUtils.TileSizeInPixels * 28f,
                UniqueIdentity = $"{nameof(SPYoyoMod)}:Code1"
            });
        }

        private void RefreshChargeValueFromTarget(NPC npc)
        {
            Projectile.Center = npc.Center;

            var size = (npc.width + npc.height) * 0.5f;
            _chargeRadius = MinChargeRadius * MathF.Sqrt(Math.Max(size / MinChargeRadius, 1f));
        }

        private bool TryGetTarget(out NPC npc)
        {
            npc = null;

            if (!Main.npc.IndexInRange(Target))
                return false;

            npc = Main.npc[Target];
            return npc is not null && npc.active && npc.life > 0;
        }

        private void TryOutlineTarget()
        {
            if (!TryGetTarget(out var npc))
                return;

            NPCEffectManager.Outline(new NPCEffectManager.OutlineSettings()
            {
                NpcWhoAmI = npc.whoAmI,
                LifeTime = InitTimeLeft,
                OutlineColor = lifeTimeRatio => GetOutlineColor(lifeTimeRatio),
                NpcColor = lifeTimeRatio => GetOutlineColor(lifeTimeRatio) * 0.1f
            });
        }

        private void SpawnChargeParticle()
        {
            if (!Main.rand.NextBool(10) || Main.dedServ)
                return;

            var radius = Radius * Main.rand.NextFloat(0.2f, 0.9f);
            var direction = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
            var color = Main.rand.NextBool() ? ChargeColor : BurstColor;
            var particle = WorldParticleManager.SpawnParticle<RotatingCubeParticle>(WorldParticleFlags.Pixelated);

            particle.LifeTime = GeneralUtils.SecondsToTicks(0.8f);
            particle.Position = Projectile.Center + direction * radius;
            particle.Velocity = direction * Main.rand.NextFloat(0.5f, 1.8f);
            particle.StartColor = color;
            particle.EndColor = color;
            particle.Scale = Main.rand.NextFloat(0.45f, 0.7f);
        }
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class Code1ScreenEffectHandler : ILoadable
    {
        public static readonly string FilterName = $"{nameof(SPYoyoMod)}:Code1DigitalWave";

        private readonly ProjectileObserver _projObserver = ProjectileObserver.Create(p => p.ModProjectile is not Code1DigitalWaveProjectile);
        private DigitalWaveShaderData _shaderData;

        public void Add(Projectile proj)
        {
            _projObserver.Add(proj);
        }

        public void Remove(Projectile proj)
        {
            _projObserver.Remove(proj);
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            _shaderData = new DigitalWaveShaderData(Code1Assets.ScreenEffect, "Code1DigitalWave");

            Filters.Scene[FilterName] = new Filter(_shaderData, EffectPriority.High);

            ModEvents.OnPostUpdateCameraPosition += UpdateFilter;
            ModEvents.OnWorldUnload += _projObserver.Clear;
        }

        void ILoadable.Unload()
        {
            ModEvents.OnWorldUnload -= _projObserver.Clear;
            ModEvents.OnPostUpdateCameraPosition -= UpdateFilter;
        }

        private void UpdateFilter()
        {
            var filter = Filters.Scene[FilterName];

            if (!_projObserver.AnyEntity)
            {
                if (!filter.IsActive())
                    return;

                _shaderData.Wave0 = Vector4.Zero;
                _shaderData.Wave1 = Vector4.Zero;
                _shaderData.Wave2 = Vector4.Zero;
                _shaderData.WaveFill = Vector3.Zero;

                filter.GetShader().UseIntensity(0f);
                filter.Deactivate();

                return;
            }

            var waves = new Wave[3];
            var index = 0;

            foreach (var proj in _projObserver.GetEntityInstances())
            {
                if (index >= waves.Length)
                    break;

                var wave = proj.As<Code1DigitalWaveProjectile>();

                if (wave is null)
                    continue;

                waves[index] = new Wave(wave.Projectile.Center, wave.Radius, wave.Strength, wave.Fill);
                index++;
            }

            _shaderData.SetWaves(waves);

            if (!filter.IsActive())
                Filters.Scene.Activate(FilterName);

            filter.GetShader().UseIntensity(1f);
            filter.Opacity = 1f;
        }

        private readonly record struct Wave(Vector2 Center, float Radius, float Strength, float Fill);

        private sealed class DigitalWaveShaderData(Asset<Effect> shader, string passName) : ScreenShaderData(shader, passName)
        {
            public Vector4 Wave0;
            public Vector4 Wave1;
            public Vector4 Wave2;
            public Vector3 WaveFill;

            public void SetWaves(Wave[] waves)
            {
                Wave0 = ToShaderWave(waves[0]);
                Wave1 = ToShaderWave(waves[1]);
                Wave2 = ToShaderWave(waves[2]);
                WaveFill = new Vector3(waves[0].Fill, waves[1].Fill, waves[2].Fill);
            }

            public override void Apply()
            {
                var offScreen = new Vector2(Main.offScreenRange);

                Shader.Parameters["Wave0"]?.SetValue(WithOffScreen(Wave0, offScreen));
                Shader.Parameters["Wave1"]?.SetValue(WithOffScreen(Wave1, offScreen));
                Shader.Parameters["Wave2"]?.SetValue(WithOffScreen(Wave2, offScreen));
                Shader.Parameters["WaveFill"]?.SetValue(WaveFill);

                base.Apply();
            }

            private static Vector4 ToShaderWave(Wave wave)
                => new(wave.Center.X, wave.Center.Y, wave.Radius, wave.Strength);

            private static Vector4 WithOffScreen(Vector4 wave, Vector2 offScreen)
                => new(wave.X - offScreen.X, wave.Y - offScreen.Y, wave.Z, wave.W);
        }
    }
}
