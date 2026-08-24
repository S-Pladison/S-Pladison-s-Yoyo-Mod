using SPYoyoMod.Common.Yoyos;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class FormatCAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/FormatC/FormatC";
    }

    public sealed class FormatCItem : YoyoItem<FormatCProjectile>
    {
        public override int OverrideType => ItemID.FormatC;
    }

    public sealed class FormatCProjectile : YoyoProjectile<FormatCItem>
    {
        public override int OverrideType => ProjectileID.FormatC;
    }
}
