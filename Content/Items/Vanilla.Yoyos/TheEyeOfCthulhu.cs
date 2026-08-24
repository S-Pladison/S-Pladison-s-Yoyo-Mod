using SPYoyoMod.Common.Yoyos;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class TheEyeOfCthulhuAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/TheEyeOfCthulhu/TheEyeOfCthulhu";
    }

    public sealed class TheEyeOfCthulhuItem : YoyoItem<TheEyeOfCthulhuProjectile>
    {
        public override int OverrideType => ItemID.TheEyeOfCthulhu;
    }

    public sealed class TheEyeOfCthulhuProjectile : YoyoProjectile<TheEyeOfCthulhuItem>
    {
        public override int OverrideType => ProjectileID.TheEyeOfCthulhu;
    }
}
