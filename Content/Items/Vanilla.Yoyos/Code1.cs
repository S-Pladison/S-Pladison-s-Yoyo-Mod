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
        public const string StringPath = $"{AssetPath}/FishingLine_WithShadow";

        public static readonly LazyAsset<Texture2D> GlowTexture = LazyAsset<Texture2D>.From($"{AssetPath}/YoyoGlow_WithShadow");
        public static readonly LazyAsset<Effect> ScreenEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Screen");
        public static readonly SoundStyle InfectSound = new("Terraria/Sounds/Item_182");
        public static readonly SoundStyle BurstSound = SoundID.Item77;
    }

    public sealed class Code1Item : VanillaYoyoBaseItem
    {
        public static readonly int WaveCooldown = GeneralUtils.SecondsToTicks(2f);
        public static readonly int WaveApplyChanceDenominator = 5;
        public static readonly int WaveMinRemainingHits = 3;

        public override int ItemType => ItemID.Code1;

        public override void SetDefaults(Item item)
        {
            item.crit = 11;
        }
    }

    public sealed class Code1Projectile : VanillaYoyoBaseProjectile, IInitializableProjectile, IEmitLightEntity
    {
        public static readonly Color GlowColor = new(40, 230, 220);

        private YoyoStringRenderer _stringRenderer;

        public override int ProjType => ProjectileID.Code1;
        public override bool InstancePerEntity => true;

        void IInitializableProjectile.Initialize(Projectile _)
        {
            if (Main.dedServ)
                return;

            _stringRenderer = new YoyoStringRenderer(new IDrawYoyoStringSegments.Gradient(
                ModContent.Request<Texture2D>(Code1Assets.StringPath, AssetRequestMode.ImmediateLoad).Value,
                (Color.Transparent, true), (Color.Transparent, true), (GlowColor, true)
            ));
        }

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.rand.NextBool(Code1Item.WaveApplyChanceDenominator))
                return;

            var remainingLife = target.IsChild(out var parent) ? parent.life : target.life;

            if (remainingLife <= damageDone * Code1Item.WaveMinRemainingHits)
                return;

            var owner = proj.GetOwner();
            var code1Player = owner.GetModPlayer<Code1Player>();

            if (code1Player.WaveCooldown > 0)
                return;

            var waveType = ModContent.ProjectileType<Code1DigitalWaveProjectile>();

            if (owner.ownedProjectileCounts[waveType] > 0)
                return;

            Projectile.NewProjectile(proj.GetSource_OnHit(target), target.Center, Vector2.Zero, waveType, proj.damage, 0f, proj.owner, target.whoAmI);

            code1Player.SetWaveCooldown();
        }

        void IEmitLightEntity.EmitLight(Entity proj)
        {
            Lighting.AddLight(proj.Center, GlowColor.ToVector3() * 0.2f);
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            var glowPosition = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var glowTexture = Code1Assets.GlowTexture.Value;
            var glowOrigin = glowTexture.Size() * 0.5f;
            var glowScale = proj.scale * 1.2f;

            Main.spriteBatch.Draw(glowTexture, glowPosition, null, GlowColor, proj.rotation, glowOrigin, glowScale, SpriteEffects.None, 0f);

            return true;
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            _stringRenderer.Render(Main.spriteBatch, YoyoStringRendererContext.FromProjectile(proj, mountedCenter));
        }
    }

    public sealed class Code1Player : ModPlayer
    {
        public int WaveCooldown { get; private set; }

        public void SetWaveCooldown()
        {
            WaveCooldown = Code1Item.WaveCooldown;
        }

        public override void PostUpdate()
        {
            if (WaveCooldown > 0)
                WaveCooldown--;
        }
    }

    public sealed class Code1DigitalWaveProjectile : ModProjectile, IInitializableProjectile, IEmitLightEntity
    {
        private enum State
        {
            Appear,
            Hold,
            Compress,
            Burst
        }

        public static readonly float MinChargeRadius = TileUtils.TileSizeInPixels * 3f;
        public static readonly float MaxWaveRadius = TileUtils.TileSizeInPixels * 16f;

        private static readonly EasingBuilder _npcOutlineEasing = new(
            (EasingFunctions.OutQuad, 0.08f, 0f, 1f),
            (EasingFunctions.Linear, 0.75f, 1f, 1f),
            (EasingFunctions.InCubic, 0.17f, 1f, 0f)
        );

        private State _state = State.Appear;
        private int _stateTimer;
        private float _chargeRadius = MinChargeRadius;

        public override string Texture => Code1Assets.InvisiblePath;
        public int TargetWhoAmI => (int)Projectile.ai[0];
        public bool IsBursting => _state == State.Burst;
        public float ChargeRadius => _chargeRadius;
        public float CompressRadius => MathHelper.Max(8f, ChargeRadius * 0.12f);
        public float StateProgress => MathHelper.Clamp(_stateTimer / (float)GetStateDuration(_state), 0f, 1f);

        public float Radius => _state switch
        {
            State.Appear => ChargeRadius * EasingFunctions.OutCubic(StateProgress),
            State.Hold => ChargeRadius,
            State.Compress => MathHelper.Lerp(ChargeRadius, CompressRadius, EasingFunctions.InCubic(StateProgress)),
            State.Burst => MathHelper.Lerp(CompressRadius, MaxWaveRadius, EasingFunctions.OutExpo(StateProgress)),
            _ => ChargeRadius
        };

        public float Strength => _state switch
        {
            State.Appear => EasingFunctions.OutQuad(StateProgress),
            State.Burst when StateProgress < 0.4f => MathHelper.Lerp(1f, 0.55f, StateProgress / 0.4f),
            State.Burst => MathHelper.Lerp(0.55f, 0f, EasingFunctions.OutCubic((StateProgress - 0.4f) / 0.6f)),
            _ => 1f
        };

        public float Fill => IsBursting ? 0f : 1f; //< Кольцо или круг

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)MaxWaveRadius;
        }

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = (int)(MaxWaveRadius * 2);
            Projectile.height = (int)(MaxWaveRadius * 2);
            Projectile.timeLeft = GetStateDuration(State.Appear) + GetStateDuration(State.Hold) + GetStateDuration(State.Compress) + GetStateDuration(State.Burst);
            Projectile.hide = true;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        void IInitializableProjectile.Initialize(Projectile _)
        {
            if (TryGetTarget(out var npc))
                RefreshChargeFromTarget(npc);

            if (Main.dedServ)
                return;

            TryOutlineTarget();

            ModContent.GetInstance<Code1ScreenEffectHandler>()?.Add(Projectile);

            SoundEngine.PlaySound(Code1Assets.InfectSound with { Pitch = 0.7f, PitchVariance = 0.08f }, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            ModContent.GetInstance<Code1ScreenEffectHandler>()?.Remove(Projectile);
        }

        public override void AI()
        {
            if (!IsBursting)
            {
                if (TryGetTarget(out var npc))
                    RefreshChargeFromTarget(npc);
                else
                    SetState(State.Burst);
            }

            TickState();

            if (Main.dedServ || IsBursting)
                return;

            SpawnChargeParticle();
        }

        public override bool ShouldUpdatePosition()
            => false;

        public override bool? CanDamage()
            => IsBursting;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CollisionUtils.CheckRectanglevCircle(targetHitbox, Projectile.Center, Radius);

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X >= Projectile.Center.X ? 1 : -1;
            modifiers.SetCrit();
        }

        public override bool? CanCutTiles()
            => false;

        void IEmitLightEntity.EmitLight(Entity _)
        {
            Lighting.AddLight(Projectile.Center, GetBurstGlowColor().ToVector3() * Strength * 0.35f);
        }

        private static int GetStateDuration(State state) => state switch
        {
            State.Appear => GeneralUtils.SecondsToTicks(0.12f),
            State.Hold => GeneralUtils.SecondsToTicks(0.88f),
            State.Compress => GeneralUtils.SecondsToTicks(0.2f),
            State.Burst => GeneralUtils.SecondsToTicks(0.8f),
            _ => 1
        };

        private void TickState()
        {
            _stateTimer++;

            if (_stateTimer < GetStateDuration(_state))
                return;

            switch (_state)
            {
                case State.Appear:
                    SetState(State.Hold);
                    break;
                case State.Hold:
                    SetState(State.Compress);
                    break;
                case State.Compress:
                    SetState(State.Burst);
                    break;
                case State.Burst:
                    Projectile.Kill();
                    break;
            }
        }

        private void SetState(State state)
        {
            var startBurst = state == State.Burst && _state != State.Burst;

            _state = state;
            _stateTimer = 0;

            if (startBurst)
                OnStartBurst();
        }

        private void RefreshChargeFromTarget(NPC npc)
        {
            Projectile.Center = npc.Center;

            var size = (npc.width + npc.height) * 0.5f;
            _chargeRadius = MinChargeRadius * MathF.Sqrt(Math.Max(size / MinChargeRadius, 1f));
        }

        private void OnStartBurst()
        {
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

            SpawnExplosionParticles();
        }

        private bool TryGetTarget(out NPC npc)
        {
            npc = null;

            if (!Main.npc.IndexInRange(TargetWhoAmI))
                return false;

            npc = Main.npc[TargetWhoAmI];
            return npc is not null && npc.active && npc.life > 0;
        }

        private void TryOutlineTarget()
        {
            if (!TryGetTarget(out var npc))
                return;

            NPCEffectManager.Outline(new NPCEffectManager.OutlineSettings()
            {
                NpcWhoAmI = npc.whoAmI,
                LifeTime = GetStateDuration(State.Appear) + GetStateDuration(State.Hold) + GetStateDuration(State.Compress) + GetStateDuration(State.Burst) / 2,
                OutlineColor = lifeTimeRatio => GetBurstGlowColor() * _npcOutlineEasing.Evaluate(lifeTimeRatio),
                NpcColor = lifeTimeRatio => GetBurstGlowColor() * (_npcOutlineEasing.Evaluate(lifeTimeRatio) * MathHelper.Lerp(0.05f, 0.4f, GetBurstRedden()))
            });
        }

        private float GetBurstRedden()
        {
            if (!IsBursting)
                return 0f;

            return EasingFunctions.OutCubic(MathHelper.Clamp(StateProgress / 0.3f, 0f, 1f));
        }

        private Color GetBurstGlowColor()
            => Color.Lerp(Code1Projectile.GlowColor, new(215, 36, 62), GetBurstRedden());

        private void SpawnChargeParticle()
        {
            if (!Main.rand.NextBool(3))
                return;

            var speedScale = _state == State.Compress ? -1.2f : 0.35f;
            var radius = Radius * Main.rand.NextFloat(0.2f, 0.9f);
            var direction = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
            var color = Main.rand.NextBool() ? Code1Projectile.GlowColor : new Color(148, 18, 48);
            var particle = WorldParticleManager.SpawnParticle<LightPointParticle>(WorldParticleFlags.Pixelated);

            particle.LifeTime = GeneralUtils.SecondsToTicks(0.4f);
            particle.Position = Projectile.Center + direction * radius;
            particle.Velocity = direction * Main.rand.NextFloat(0.5f, 1.8f) * speedScale;
            particle.StartColor = color;
            particle.EndColor = color;
            particle.Scale = Main.rand.NextFloat(0.2f, 0.4f);
        }

        private void SpawnExplosionParticles()
        {
            const int count = 14;

            for (var i = 0; i < count; i++)
            {
                var angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.2f, 0.2f);
                var direction = Vector2.UnitX.RotatedBy(angle);
                var color = Main.rand.NextBool() ? Code1Projectile.GlowColor : new Color(148, 18, 48);
                var particle = WorldParticleManager.SpawnParticle<LightPointParticle>(WorldParticleFlags.Pixelated);

                particle.LifeTime = GeneralUtils.SecondsToTicks(0.4f);
                particle.Position = Projectile.Center;
                particle.Velocity = direction * Main.rand.NextFloat(2.8f, 5.5f);
                particle.StartColor = color;
                particle.EndColor = color;
                particle.Scale = Main.rand.NextFloat(0.25f, 0.5f);
            }
        }
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class Code1ScreenEffectHandler : ILoadable
    {
        public readonly record struct ScreenWave(Vector2 Center, float Radius, float Strength, float Fill);

        private const string FilterName = $"{nameof(SPYoyoMod)}:Code1DigitalWave";

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

            var waves = new ScreenWave[3];
            var index = 0;

            foreach (var proj in _projObserver.GetEntityInstances())
            {
                if (index >= waves.Length)
                    break;

                var wave = proj.As<Code1DigitalWaveProjectile>();

                if (wave is null)
                    continue;

                waves[index] = new ScreenWave(wave.Projectile.Center, wave.Radius, wave.Strength, wave.Fill);
                index++;
            }

            _shaderData.SetWaves(waves);

            if (!filter.IsActive())
                Filters.Scene.Activate(FilterName);

            filter.GetShader().UseIntensity(1f);
            filter.Opacity = 1f;
        }

        private sealed class DigitalWaveShaderData(Asset<Effect> shader, string passName) : ScreenShaderData(shader, passName)
        {
            public Vector4 Wave0;
            public Vector4 Wave1;
            public Vector4 Wave2;
            public Vector3 WaveFill;

            public void SetWaves(ScreenWave[] waves)
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

            private static Vector4 ToShaderWave(ScreenWave wave)
                => new(wave.Center.X, wave.Center.Y, wave.Radius, wave.Strength);

            private static Vector4 WithOffScreen(Vector4 wave, Vector2 offScreen)
                => new(wave.X - offScreen.X, wave.Y - offScreen.Y, wave.Z, wave.W);
        }
    }
}
