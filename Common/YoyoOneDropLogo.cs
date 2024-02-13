using SPYoyoMod.Utils.Entities;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public class YoyoOneDropLogoGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return item.IsYoyo();
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            var oneDropLogoTooltip = tooltips.Find(i => i.Name == "OneDropLogo");

            if (oneDropLogoTooltip is not null)
                tooltips.Remove(oneDropLogoTooltip);
        }
    }
}