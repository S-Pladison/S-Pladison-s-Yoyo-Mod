using SPYoyoMod.Common.Yoyos;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class YeletsAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Yelets/Yelets";
    }

    public sealed class YeletsItem : YoyoItem<YeletsProjectile>
    {
        public override int OverrideType => ItemID.Yelets;
    }

    public sealed class YeletsProjectile : YoyoProjectile<YeletsItem>
    {
        public override int OverrideType => ProjectileID.Yelets;
    }
}
