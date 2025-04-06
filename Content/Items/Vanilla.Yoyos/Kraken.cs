using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class KrakenAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Kraken/Kraken";
    }

    public sealed class KrakenItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Kraken;
    }

    public sealed class KrakenProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Kraken;
    }
}