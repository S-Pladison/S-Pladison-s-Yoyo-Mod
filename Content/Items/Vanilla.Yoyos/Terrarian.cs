using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class TerrarianAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Terrarian/Terrarian";
    }

    public sealed class TerrarianItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Terrarian;
    }

    public sealed class TerrarianProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Terrarian;
    }
}