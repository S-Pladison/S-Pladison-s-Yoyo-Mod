using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class RallyAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Rally/Rally";
    }

    public sealed class RallyItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Rally;
    }

    public sealed class RallyProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Rally;
    }
}