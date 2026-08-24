using SPYoyoMod.Common.Yoyos;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class AmarokAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Amarok/Amarok";
    }

    public sealed class AmarokItem : YoyoItem<AmarokProjectile>
    {
        public override int OverrideType => ItemID.Amarok;
    }

    public sealed class AmarokProjectile : YoyoProjectile<AmarokItem>
    {
        public override int OverrideType => ProjectileID.Amarok;
    }
}
