using SPYoyoMod.Utils;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class YoyoOneDropLogoGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
            => lateInstantiation && item.IsYoyo();

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
            => tooltips.Find(VanillaTooltipLine.OneDropLogo).Hide();
    }
}