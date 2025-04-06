using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class FormatCAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/FormatC/FormatC";
    }

    public sealed class FormatCItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.FormatC;
    }

    public sealed class FormatCProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.FormatC;
    }
}