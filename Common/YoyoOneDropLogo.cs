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
            return lateInstantiation && item.IsYoyo();
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Find(i => i.Mod == "Terraria" && i.Name == "OneDropLogo")?.Hide();
        }
    }
}