using SPYoyoMod.Common.Hooks;
using Terraria;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class CascadeItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Cascade;
    }

    public sealed class CascadeProjectile : VanillaYoyoBaseProjectile, IInitializableProjectile
    {
        public override int ProjType => ProjectileID.Cascade;

        public void Initialize(Projectile proj)
        {
            Main.NewText("Каскад был заспавнен!");
        }
    }
}