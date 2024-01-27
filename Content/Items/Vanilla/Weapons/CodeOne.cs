using SPYoyoMod.Utils;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class CodeOneItem : VanillaYoyoItem
    {
        public static LocalizedText Tooltip { get; private set; }

        public CodeOneItem() : base(ItemID.Code1) { }

        public override void SetStaticDefaults()
        {
            Tooltip = Language.GetOrRegister("Mods.SPYoyoMod.VanillaItems.Code1Item.Tooltip");
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
    }

    public class CodeOneProjectile : VanillaYoyoProjectile
    {
        public CodeOneProjectile() : base(ProjectileID.Code1) { }
    }
}