using SPYoyoMod.Common.Yoyos;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class MalaiseAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Malaise/Malaise";
    }

    public sealed class MalaiseItem : YoyoItem<MalaiseProjectile>
    {
        public override int OverrideType => ItemID.CorruptYoyo;
    }

    public sealed class MalaiseProjectile : YoyoProjectile<MalaiseItem>
    {
        public override int OverrideType => ProjectileID.CorruptYoyo;
    }
}
