using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class CascadeItem : VanillaYoyoItem
    {
        public CascadeItem() : base(ItemID.Cascade) { }

        public override void SetStaticDefaults()
        {
            // Because this yoyo cannot be obtained in hardmode (since Hel-Fire drops instead)
            // * Rework required when tModLoader update shimmer system
            ModEvents.OnHardmodeStart += AddShimmerTransforms;
            ModEvents.OnWorldLoad += () => { if (Main.hardMode) AddShimmerTransforms(); };
            ModEvents.OnWorldUnload += RemoveShimmerTransforms;
        }

        private static void AddShimmerTransforms()
        {
            ItemID.Sets.ShimmerTransformToItem[ItemID.Cascade] = ItemID.HelFire;
            ItemID.Sets.ShimmerTransformToItem[ItemID.HelFire] = ItemID.Cascade;
        }

        private static void RemoveShimmerTransforms()
        {
            ItemID.Sets.ShimmerTransformToItem[ItemID.Cascade] = -1;
            ItemID.Sets.ShimmerTransformToItem[ItemID.HelFire] = -1;
        }
    }

    public class CascadeProjectile : VanillaYoyoProjectile
    {
        public override bool InstancePerEntity { get => true; }

        public CascadeProjectile() : base(ProjectileID.Cascade) { }

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            // Кароче, переодически взрывается, нанося урон по области... Эффекты накопления взрыва могут юзать lineRenderer,
            // ток над реализовать метод Offset чтобы сетку линии не генерить постоянно... Взрывается только основной йо-йо.
        }

        public override void AI(Projectile proj)
        {

        }
    }
}