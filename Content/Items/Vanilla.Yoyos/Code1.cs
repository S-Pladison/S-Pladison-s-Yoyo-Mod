using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class Code1Assets : ILoadable
    {
        public const string InvisiblePath = $"{_assetPath}Invisible";

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Code1/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
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
    }

    public sealed class Code1ImprovedCritProjectile : ModProjectile
    {
        public override string Texture { get => Code1Assets.InvisiblePath; }

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = 48;
            Projectile.height = 48;

            Projectile.timeLeft = 60;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
    }
}