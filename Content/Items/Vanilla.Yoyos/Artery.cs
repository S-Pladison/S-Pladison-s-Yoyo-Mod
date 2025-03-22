using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class ArteryAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Artery/Artery";
    }

    public sealed class ArteryItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.CrimsonYoyo;
    }

    public sealed class ArteryProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.CrimsonYoyo;
    }
}