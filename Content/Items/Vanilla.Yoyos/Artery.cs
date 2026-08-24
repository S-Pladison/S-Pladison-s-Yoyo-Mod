using SPYoyoMod.Common.Yoyos;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class ArteryAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Artery/Artery";
    }

    public sealed class ArteryItem : YoyoItem<ArteryProjectile>
    {
        public override int OverrideType => ItemID.CrimsonYoyo;
    }

    public sealed class ArteryProjectile : YoyoProjectile<ArteryItem>
    {
        public override int OverrideType => ProjectileID.CrimsonYoyo;
    }
}
