using System;
using Microsoft.Xna.Framework;
using SPYoyoMod.Content.Items.Mod.Yoyos;
using SPYoyoMod.Core.Graphics;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class Code1Assets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Code1/Code1";
    }

    public sealed class Code1Item : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Code1;

        public override void SetDefaults(Item item)
        {
            item.crit = 16;
        }
    }

    public sealed class Code1Projectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Code1;

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!proj.TryGetOwner(out var owner))
                return;

            var modPlayer = owner.GetModPlayer<Code1Player>();
            
            if (!modPlayer.CanSpawnSphere)
                return;

            modPlayer.StartSphereCooldown();

            Projectile.NewProjectile(proj.GetSource_OnHit(target), target.Center, Vector2.Zero, ModContent.ProjectileType<Code1SphereProjectile>(), proj.damage, proj.knockBack, proj.owner, 0, target.whoAmI);
            Projectile.NewProjectile(proj.GetSource_OnHit(target), target.Center, Vector2.Zero, ModContent.ProjectileType<Code1SphereProjectile>(), proj.damage, proj.knockBack, proj.owner, 1, target.whoAmI);
        }
    }

    public sealed class Code1SphereProjectile : ModProjectile, IInitializableProjectile
    {
        public static readonly int InitTimeLeft = GeneralUtils.SecondsToTicks(2.5f);
        public static readonly int SpinCount = 1;

        public static readonly float MaxDistanceFromTarget = TileUtils.TileSizeInPixels * 4.5f;
        public static readonly EasingBuilder DistanceFromTargetEasing = new(
            (EasingFunctions.InOutQuad, 0.1f, 0f, 1f),
            (EasingFunctions.Linear, 0.8f, 1f, 1f),
            (EasingFunctions.InOutQuad, 0.1f, 1f, 0f)
        );

        public static readonly int NpcHitOutlineLifeTime = GeneralUtils.SecondsToTicks(0.15f);
        public static readonly float MaxNpcHitOutlineThickness = 1.2f;
        public static readonly EasingBuilder NpcHitOutlineThicknessEasing = new(
            (EasingFunctions.InOutExpo, 0.2f, 0f, 1f),
            (EasingFunctions.InOutQuad, 0.8f, 1f, 0f)
        );

        public override string Texture => BellowingThunderAssets.InvisiblePath;
        public int SphereIndex => (int)Projectile.ai[0];
        public int TargetWhoAmI => (int)Projectile.ai[1];
        public NPC Target => TargetWhoAmI < 0 || TargetWhoAmI >= Main.maxNPCs ? null : Main.npc[TargetWhoAmI];
        public float LifeTimeRatio { get => 1f - Projectile.timeLeft / (float)InitTimeLeft; }

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = 10;
            Projectile.height = 10;

            Projectile.timeLeft = InitTimeLeft;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
        }

        void IInitializableProjectile.Initialize(Projectile proj)
        {
            if (Main.netMode == NetmodeID.Server)
                return;
        }

        public override void AI()
        {
            // TODO: Нужно добавить логику на случай, если цель умрет раньше времени жизни сферы
            if (!Target?.active ?? true)
            {
                Projectile.Kill();
                return;
            }

            var angleOffset = SphereIndex == 0 ? 0f : MathHelper.Pi;
            var orbitRadius = DistanceFromTargetEasing.Evaluate(LifeTimeRatio) * MaxDistanceFromTarget;
            var angle = LifeTimeRatio * SpinCount * MathHelper.TwoPi + angleOffset;

            // TODO: Вращение должно также начинаться и заканчиваться плавно
            //       Расстояние до цели должно быть более проработанным
            Projectile.Center = Target.Center + Target.netOffset + Vector2.UnitX.RotatedBy(angle) * orbitRadius;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.Knockback *= 0.1f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.TryGetOwner(out var owner))
                owner.Counterweight(target.Center, Projectile.damage, Projectile.knockBack);

            if (SphereIndex != 0)
                return;

            // TODO: Вызывать эффект только при соприкосновении с лучем
            NPCEffectManager.Outline(new NPCEffectManager.OutlineSettings()
            {
                NpcWhoAmI = target.whoAmI,
                LifeTime = NpcHitOutlineLifeTime,
                OutlineThickness = static (lifeTimeRatio) => MaxNpcHitOutlineThickness * NpcHitOutlineThicknessEasing.Evaluate(lifeTimeRatio),
                OutlineColor = static (lifeTimeRatio) => new Color(65, 185, 255) * NpcHitOutlineThicknessEasing.Evaluate(lifeTimeRatio)
            });
        }
    }

    public sealed class Code1Player : ModPlayer
    {
        public static readonly int SphereCooldown = GeneralUtils.SecondsToTicks(4f);

        private int _sphereTimer;

        public bool CanSpawnSphere => _sphereTimer == 0;

        public override void PostUpdate()
        {
            if (_sphereTimer > 0)
            {
                _sphereTimer--;
            }
        }

        public void StartSphereCooldown()
        {
            _sphereTimer = SphereCooldown;
        }
    }
}