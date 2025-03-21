using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class HelFireAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/HelFire/HelFire";
    }

    public sealed class HelFireItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.HelFire;
    }

    public sealed class HelFireProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.HelFire;
    }
}