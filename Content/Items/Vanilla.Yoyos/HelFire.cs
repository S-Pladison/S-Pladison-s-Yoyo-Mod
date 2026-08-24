using SPYoyoMod.Common.Yoyos;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class HelFireAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/HelFire/HelFire";
    }

    public sealed class HelFireItem : YoyoItem<HelFireProjectile>
    {
        public override int OverrideType => ItemID.HelFire;
    }

    public sealed class HelFireProjectile : YoyoProjectile<HelFireItem>
    {
        public override int OverrideType => ProjectileID.HelFire;
    }
}
