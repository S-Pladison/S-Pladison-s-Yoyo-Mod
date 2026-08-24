using SPYoyoMod.Common.Yoyos;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class Code2Assets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Code2/Code2";
    }

    public sealed class Code2Item : YoyoItem<Code2Projectile>
    {
        public override int OverrideType => ItemID.Code2;
    }

    public sealed class Code2Projectile : YoyoProjectile<Code2Item>
    {
        public override int OverrideType => ProjectileID.Code2;
    }
}
