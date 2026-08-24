using SPYoyoMod.Common.Yoyos;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class HiveFiveAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/HiveFive/HiveFive";
    }

    public sealed class HiveFiveItem : YoyoItem<HiveFiveProjectile>
    {
        public override int OverrideType => ItemID.HiveFive;
    }

    public sealed class HiveFiveProjectile : YoyoProjectile<HiveFiveItem>
    {
        public override int OverrideType => ProjectileID.HiveFive;
    }
}
