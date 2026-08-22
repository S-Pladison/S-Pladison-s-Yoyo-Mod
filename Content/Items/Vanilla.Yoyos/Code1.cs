using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Content.Particles;
using SPYoyoMod.Core.Graphics;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
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
    }

    public sealed class Code1Item : VanillaYoyoBaseItem
    {
        public static readonly int WaveCooldown = GeneralUtils.SecondsToTicks(6f);

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
            if (!hit.Crit)
                return;

            if (!proj.IsLocalPlayerAsOwner())
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
        public static readonly int InitTimeLeft = GeneralUtils.SecondsToTicks(0.65f);
        public static readonly int NpcOutlineLifeTime = GeneralUtils.SecondsToTicks(0.5f);
        public static readonly float MaxRadius = TileUtils.TileSizeInPixels * 16f;
        public static readonly Color GlowColor = Code1Projectile.GlowColor;
        public static readonly Color GlitchColor = new(255, 55, 190);
        public static readonly EasingBuilder StrengthEasing = new(
            (EasingFunctions.OutQuad, 0.1f, 0f, 1f),
            (EasingFunctions.Linear, 0.18f, 1f, 0.75f),
            (EasingFunctions.InQuad, 0.32f, 0.75f, 0.18f),
            (EasingFunctions.InCubic, 0.4f, 0.18f, 0f)
        );
        public static readonly EasingBuilder NpcOutlineEasing = new(
            (EasingFunctions.OutQuad, 0.12f, 0f, 1f),
            (EasingFunctions.Linear, 0.48f, 1f, 0.8f),
            (EasingFunctions.InCubic, 0.4f, 0.8f, 0f)
        );

        public override string Texture => Code1Assets.InvisiblePath;
        public int TargetWhoAmI => (int)Projectile.ai[0];
        public float LifeTimeRatio => 1f - Projectile.timeLeft / (float)InitTimeLeft;
        public float CurrentRadius => MaxRadius * EasingFunctions.OutCubic(LifeTimeRatio);
        public float Strength => StrengthEasing.Evaluate(LifeTimeRatio);

        public override void SetDefaults()
        {
            Projectile.DefaultToVisualEffect();

            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.timeLeft = InitTimeLeft;
            Projectile.hide = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        void IInitializableProjectile.Initialize(Projectile _)
        {
            if (Main.dedServ)
                return;

            TryOutlineTarget();

            ModContent.GetInstance<Code1ScreenEffectHandler>()?.Add(Projectile);

            SoundEngine.PlaySound(SoundID.Item77 with { Pitch = 0.55f, PitchVariance = 0.12f, Volume = 0.4f }, Projectile.Center);

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

            for (var i = 0; i < 10; i++)
                SpawnWaveParticle(0f, 1.8f);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            ModContent.GetInstance<Code1ScreenEffectHandler>()?.Remove(Projectile);
        }

        public override void AI()
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextBool(2))
                SpawnWaveParticle(CurrentRadius, 1.1f);
        }

        public override bool ShouldUpdatePosition()
            => false;

        public override bool? CanDamage()
            => true;

        public override bool? CanHitNPC(NPC target)
        {
            if (target.whoAmI == TargetWhoAmI)
                return false;

            return null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            var closest = Terraria.Utils.ClosestPointInRect(targetHitbox, Projectile.Center);
            var radius = CurrentRadius;

            return Vector2.DistanceSquared(Projectile.Center, closest) <= radius * radius;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X >= Projectile.Center.X ? 1 : -1;
        }

        public override bool? CanCutTiles()
            => false;

        void IEmitLightEntity.EmitLight(Entity _)
        {
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * Strength * 0.35f);
        }

        public Vector4 PackScreenWave()
            => new(Projectile.Center.X, Projectile.Center.Y, CurrentRadius, Strength);

        private void TryOutlineTarget()
        {
            if (!Main.npc.IndexInRange(TargetWhoAmI))
                return;

            var npc = Main.npc[TargetWhoAmI];

            if (npc is null || !npc.active || npc.life <= 0)
                return;

            NPCEffectManager.Outline(new NPCEffectManager.OutlineSettings()
            {
                NpcWhoAmI = npc.whoAmI,
                LifeTime = NpcOutlineLifeTime,
                OutlineColor = static (lifeTimeRatio) => GlowColor * NpcOutlineEasing.Evaluate(lifeTimeRatio),
                NpcColor = static (lifeTimeRatio) => GlowColor * (NpcOutlineEasing.Evaluate(lifeTimeRatio) * 0.05f)
            });
        }

        private void SpawnWaveParticle(float radius, float speedScale)
        {
            var angle = Main.rand.NextFloat(MathHelper.TwoPi);
            var direction = Vector2.UnitX.RotatedBy(angle);
            var color = Main.rand.NextBool() ? GlowColor : GlitchColor;
            var particle = WorldParticleManager.SpawnParticle<LightPointParticle>(WorldParticleFlags.Pixelated);

            particle.LifeTime = GeneralUtils.SecondsToTicks(0.4f);
            particle.Position = Projectile.Center + direction * radius;
            particle.Velocity = direction * Main.rand.NextFloat(0.5f, 1.8f) * speedScale;
            particle.StartColor = color;
            particle.EndColor = color;
            particle.Scale = Main.rand.NextFloat(0.2f, 0.4f);
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

                filter.GetShader().UseIntensity(0f);
                filter.Deactivate();

                return;
            }

            var packed = new Vector4[3];
            var index = 0;

            foreach (var proj in _projObserver.GetEntityInstances())
            {
                if (index >= packed.Length)
                    break;

                var wave = proj.As<Code1DigitalWaveProjectile>();

                if (wave is null)
                    continue;

                packed[index++] = wave.PackScreenWave();
            }

            _shaderData.Wave0 = packed[0];
            _shaderData.Wave1 = packed[1];
            _shaderData.Wave2 = packed[2];

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

            public override void Apply()
            {
                var offScreen = new Vector2(Main.offScreenRange);

                Shader.Parameters["Wave0"]?.SetValue(WithOffScreen(Wave0, offScreen));
                Shader.Parameters["Wave1"]?.SetValue(WithOffScreen(Wave1, offScreen));
                Shader.Parameters["Wave2"]?.SetValue(WithOffScreen(Wave2, offScreen));

                base.Apply();
            }

            private static Vector4 WithOffScreen(Vector4 wave, Vector2 offScreen)
                => new(wave.X - offScreen.X, wave.Y - offScreen.Y, wave.Z, wave.W);
        }
    }
}
