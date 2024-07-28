using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Graphics;
using SPYoyoMod.Common.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class CascadeAssets : ILoadable
    {
        // [ Текстуры ]
        public const string InvisiblePath = $"{nameof(SPYoyoMod)}/Assets/Invisible";
        public const string StringPath = $"{nameof(SPYoyoMod)}/Assets/FishingLine_WithShadow";
        public static Asset<Texture2D> ExplosionRingTexture { get; private set; } = ModContent.Request<Texture2D>($"{_path}CascadeExplosion");

        // [ Эффекты ]
        public static Asset<Effect> ExplosionRingEffect { get; private set; } = ModContent.Request<Effect>($"{_path}CascadeExplosionShader");

        // [ Звуки ]
        public static readonly SoundStyle StartChargingSound = new($"{_path}CascadeSound_StartCharging");

        // [ Общее ]
        private const string _path = $"{nameof(SPYoyoMod)}/Assets/Items/Vanilla.Yoyos/Cascade/";

        void ILoadable.Unload()
        {
            ExplosionRingTexture = null;
            ExplosionRingEffect = null;
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) {}
    }

    public sealed class CascadeItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Cascade;
    }

    public sealed class CascadeProjectile : VanillaYoyoBaseProjectile, IInitializableProjectile, IPreDrawPixelatedProjectile
    {
        private enum AIStates
        {
            NonActive,
            Explodes
        }

        public static readonly float StartToChargeTime = ModUtils.SecondsToTicks(2f);
        public static readonly float ChargeTime = ModUtils.SecondsToTicks(0.7f);

        private StateMachine<AIStates> _aiStateMachine;
        private int _aiTimer;
        private YoyoStringRenderer _stringRenderer;
        //private StripRenderer _trailRenderer;

        public override int ProjType => ProjectileID.Cascade;
        public override bool InstancePerEntity => true;

        public void Initialize(Projectile proj)
        {
            InitAIStates(proj);

            if (Main.dedServ)
                return;

            _stringRenderer = new YoyoStringRenderer(proj, new IDrawYoyoStringSegment.Gradient(
                ModContent.Request<Texture2D>(CascadeAssets.StringPath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value,
                (Color.Transparent, true), (Color.Transparent, true), (new Color(255, 180, 95), true)
            ));

            //_trailRenderer = new StripRenderer();
        }

        private void InitAIStates(Projectile proj)
        {
            _aiStateMachine = new StateMachine<AIStates>();
            
            // Ждем некоторое время перед тем, как начать заряжаться
            _aiStateMachine.RegisterState(AIStates.NonActive)
              .Process(WaitingToStartCharge);
            
            // 'Заряжаемся' перед взрывом, после чего в конце создаем снаряд взрыва
            _aiStateMachine.RegisterState(AIStates.Explodes)
              .OnEnter(() => SoundEngine.PlaySound(CascadeAssets.StartChargingSound, proj.Center))
              .Process(ChargeBeforeExplosion)
              .OnExit(() => OnExplosion(proj));

            // Увеличиваем таймер для всех состояний
            _aiStateMachine.OnPreProcess += () => { _aiTimer++; };

            // Сбрасываем таймер и синхронизируем снаряд с другими клиентами
            _aiStateMachine.OnStateChanged += () =>
            {
                _aiTimer = 0;
                proj.netUpdate = true;
            };

            _aiStateMachine.SetState(AIStates.NonActive);
        }

        public override void OnKill(Projectile proj, int timeLeft)
        {
            //_trailRenderer?.Dispose();
        }

        public override void AI(Projectile proj)
        {
            _aiStateMachine.Process();

            Lighting.AddLight(proj.Center, new Color(255, 180, 95).ToVector3() * 0.25f);
        }

        private void WaitingToStartCharge(StateMachine<AIStates> aiStateMachine)
        {
            if (_aiTimer > StartToChargeTime)
                aiStateMachine.SetState(AIStates.Explodes);
        }

        private void ChargeBeforeExplosion(StateMachine<AIStates> aiStateMachine)
        {
            if (_aiTimer > ChargeTime)
                aiStateMachine.SetState(AIStates.NonActive);
        }

        private void OnExplosion(Projectile proj)
        {
            if (Main.myPlayer == proj.owner)
                Projectile.NewProjectile(proj.GetSource_FromAI(), proj.Center, Vector2.Zero, ModContent.ProjectileType<CascadeExplosionProjectile>(), proj.damage, proj.knockBack, proj.owner);

            SoundEngine.PlaySound(SoundID.Item14, proj.Center);
        }

        public override void SendExtraAI(Projectile proj, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(_aiStateMachine is not null);

            if (_aiStateMachine is null)
                return;

            binaryWriter.Write((byte)_aiStateMachine.CurrentState);
            binaryWriter.Write((ushort)_aiTimer);
        }

        public override void ReceiveExtraAI(Projectile proj, BitReader bitReader, BinaryReader binaryReader)
        {
            if (!bitReader.ReadBit())
                return;

            var state = (AIStates)binaryReader.ReadByte();

            if (state != _aiStateMachine.CurrentState)
                _aiStateMachine.SetState(state);
            
            _aiTimer = binaryReader.ReadUInt16();
        }

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (_aiStateMachine.CurrentState != AIStates.NonActive)
                return;

            _aiTimer += 5;
        }

        public void PreDrawPixelated(Projectile proj)
        {
            /*_trailRenderer?
                .SetPoints(proj.oldPos)
                .Draw();*/
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            _stringRenderer?
                .SetStartPosition(mountedCenter + proj.GetOwner()?.gfxOffY * Vector2.UnitY ?? Vector2.Zero)
                .Render();
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

            _ringRenderer = new RingRenderer();
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

            CascadeAssets.ExplosionRingEffect
                .Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(CascadeAssets.ExplosionRingTexture.Value);
                    parameters["TransformMatrix"].SetValue(GameMatrices.Effect * GameMatrices.Projection);
                    parameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.05f);
                    parameters["UvRepeat"].SetValue(3f);
                    parameters["Color0"].SetValue(new Color(255, 180, 100).ToVector4());
                    parameters["Color1"].SetValue(new Color(255, 80, 0).ToVector4());
                })
                .Apply("CascadeExplosionRing");

            _ringRenderer?.Render();
        }
    }
}