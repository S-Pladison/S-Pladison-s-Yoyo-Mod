using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class YeletsAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Yelets/Yelets";
    }

    public sealed class YeletsItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Yelets;
    }

    public sealed class YeletsProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Yelets;
    }
}