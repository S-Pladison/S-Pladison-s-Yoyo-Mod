using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class MalaiseAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Malaise/Malaise";
    }

    public sealed class MalaiseItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.CorruptYoyo;
    }

    public sealed class MalaiseProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.CorruptYoyo;
    }
}