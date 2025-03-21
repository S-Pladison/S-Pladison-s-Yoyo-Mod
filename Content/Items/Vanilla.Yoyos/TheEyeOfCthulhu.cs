using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class TheEyeOfCthulhuAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/TheEyeOfCthulhu/TheEyeOfCthulhu";
    }

    public sealed class TheEyeOfCthulhuItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.TheEyeOfCthulhu;
    }

    public sealed class TheEyeOfCthulhuProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.TheEyeOfCthulhu;
    }
}