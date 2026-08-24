using SPYoyoMod.Common.Yoyos;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class KrakenAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Kraken/Kraken";
    }

    public sealed class KrakenItem : YoyoItem<KrakenProjectile>
    {
        public override int OverrideType => ItemID.Kraken;
    }

    public sealed class KrakenProjectile : YoyoProjectile<KrakenItem>
    {
        public override int OverrideType => ProjectileID.Kraken;
    }
}
