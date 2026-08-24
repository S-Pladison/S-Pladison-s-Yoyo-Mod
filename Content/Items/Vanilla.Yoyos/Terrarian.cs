using SPYoyoMod.Common.Yoyos;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class TerrarianAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Terrarian/Terrarian";
    }

    public sealed class TerrarianItem : YoyoItem<TerrarianProjectile>
    {
        public override int OverrideType => ItemID.Terrarian;
    }

    public sealed class TerrarianProjectile : YoyoProjectile<TerrarianItem>
    {
        public override int OverrideType => ProjectileID.Terrarian;
    }
}
