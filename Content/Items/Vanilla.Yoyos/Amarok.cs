using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class AmarokAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Amarok/Amarok";
    }

    public sealed class AmarokItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Amarok;
    }

    public sealed class AmarokProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Amarok;
    }
}