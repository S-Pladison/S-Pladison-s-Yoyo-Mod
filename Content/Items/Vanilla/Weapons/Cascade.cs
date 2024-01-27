using SPYoyoMod.Utils;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class CascadeItem : VanillaYoyoItem
    {
        public static LocalizedText Tooltip { get; private set; }

        public CascadeItem() : base(ItemID.Cascade) { }

        public override void SetStaticDefaults()
        {
            Tooltip = Language.GetOrRegister("Mods.SPYoyoMod.VanillaItems.CascadeItem.Tooltip");

            // Because this yoyo cannot be obtained in hardmode (since Hel-Fire drops instead)
            // * Rework required when tModLoader update shimmer system
            ModEvents.OnHardmodeStart += AddShimmerTransforms;
            ModEvents.OnWorldLoad += () => { if (Main.hardMode) AddShimmerTransforms(); };
            ModEvents.OnWorldUnload += RemoveShimmerTransforms;
        }

        public override void Unload()
        {
            Tooltip = null;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            var description = new TooltipLine(Mod, "ModTooltip", Tooltip.Value);
            TooltipUtils.InsertDescriptions(tooltips, TooltipUtils.Split(description, '\n'));
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