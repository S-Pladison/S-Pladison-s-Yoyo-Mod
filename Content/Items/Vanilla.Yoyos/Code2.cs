using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class Code2Assets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Code2/Code2";
    }

    public sealed class Code2Item : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Code2;
    }

    public sealed class Code2Projectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Code2;
    }
}