using SPYoyoMod.Common.Yoyos;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class AmazonAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Amazon/Amazon";
    }

    public sealed class AmazonItem : YoyoItem<AmazonProjectile>
    {
        public override int OverrideType => ItemID.JungleYoyo;
    }

    public sealed class AmazonProjectile : YoyoProjectile<AmazonItem>
    {
        public override int OverrideType => ProjectileID.JungleYoyo;
    }
}
