using System.Collections.Generic;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    public static class TooltipUtils
    {
        public static TooltipLine GetDescriptionTooltipsLast(List<TooltipLine> tooltips)
            => tooltips.FindLast(i => i.Mod == "Terraria" && i.Name.StartsWith("Tooltip"));

        public static int GetDescriptionLastIndex(List<TooltipLine> tooltips)
            => tooltips.FindLastIndex(i => i.Mod == "Terraria" && i.Name.StartsWith("Tooltip"));
    }
}