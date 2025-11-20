using Microsoft.Xna.Framework;
using SPYoyoMod.Content.Items.Mod.Yoyos;
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

            Projectile.NewProjectile(proj.GetSource_OnHit(target), target.Center, Vector2.Zero, ModContent.ProjectileType<Code1SphereProjectile>(), proj.damage, proj.knockBack, proj.owner, 0);
            Projectile.NewProjectile(proj.GetSource_OnHit(target), target.Center, Vector2.Zero, ModContent.ProjectileType<Code1SphereProjectile>(), proj.damage, proj.knockBack, proj.owner, 1);
        }
    }

    public sealed class Code1SphereProjectile : ModProjectile, IInitializableProjectile
    {
        public override string Texture => BellowingThunderAssets.InvisiblePath;
        public int SphereIndex => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
        }

        void IInitializableProjectile.Initialize(Projectile proj)
        {
            if (Main.netMode == NetmodeID.Server)
                return;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.TryGetOwner(out var owner))
                owner.Counterweight(target.Center, Projectile.damage, Projectile.knockBack);
        }
    }

    public sealed class Code1Player : ModPlayer
    {
        public static readonly int SphereCooldown = GeneralUtils.SecondsToTicks(5f);

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