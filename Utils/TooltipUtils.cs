using System.Collections.Generic;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    public static class TooltipUtils
    {
        public static TooltipLine FindDescriptionLast(List<TooltipLine> tooltips)
            => tooltips.FindLast(i => i.Mod == "Terraria" && i.Name.StartsWith("Tooltip"));

        public static int FindDescriptionLastIndex(List<TooltipLine> tooltips)
            => tooltips.FindLastIndex(i => i.Mod == "Terraria" && i.Name.StartsWith("Tooltip"));
    }
}