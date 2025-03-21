using Terraria.ID;

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
}