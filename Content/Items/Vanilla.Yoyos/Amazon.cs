using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class AmazonAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Amazon/Amazon";
    }

    public sealed class AmazonItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.JungleYoyo;
    }

    public sealed class AmazonProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.JungleYoyo;
    }

    public sealed class AmazonJungleAreaProjectile : ModProjectile
    {
        public override string Texture => base.Texture;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
        }
    }
}