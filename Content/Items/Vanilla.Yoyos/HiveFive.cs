using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class HiveFiveAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/HiveFive/HiveFive";
    }

    public sealed class HiveFiveItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.HiveFive;
    }

    public sealed class HiveFiveProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.HiveFive;
    }
}