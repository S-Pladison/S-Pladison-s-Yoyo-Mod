using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public sealed class CascadeAssets
    {
        public const string InvisiblePath = $"{nameof(SPYoyoMod)}/Assets/Invisible";
        public const string StringPath = $"{nameof(SPYoyoMod)}/Assets/FishingLine_WithShadow";

        public static readonly SoundStyle StartChargingSound = new($"{_path}CascadeSound_StartCharging");

        private const string _path = $"{nameof(SPYoyoMod)}/Assets/Items/Vanilla.Yoyos/Cascade/";
    }

    public sealed class CascadeItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Cascade;

        public override void AddRecipes()
        {
            Recipe.Create(ItemID.Cascade)
                .AddIngredient(ItemID.HellstoneBar, 15)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    public sealed class CascadeProjectile : VanillaYoyoBaseProjectile, IInitializableProjectile
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

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            _stringRenderer.Draw(mountedCenter + proj.GetOwner()?.gfxOffY * Vector2.UnitY ?? Vector2.Zero);
        }
    }

    public sealed class CascadeExplosionProjectile : ModProjectile, IInitializableProjectile
    {
        public static readonly int ExplosionRadius = TileUtils.TileSizeInPixels * 6;
        public static readonly int InitTimeLeft = ModUtils.SecondsToTicks(0.33f);

        private RingRenderer _ringRenderer;

        public override string Texture => CascadeAssets.InvisiblePath;

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
            if (Main.dedServ)
                return;

            //_ringRenderer = new RingRenderer();
        }

        public override void OnKill(int timeLeft)
        {
            _ringRenderer?.Dispose();
        }
    }
}