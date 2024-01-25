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

        public static void InsertDescription(List<TooltipLine> tooltips, TooltipLine line)
        {
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                var tooltipLine = tooltips[i];

                if (tooltipLine.Mod != "Terraria")
                    continue;

                if (!tooltipLine.Name.StartsWith("Tooltip")
                    && !InsertDescriptionWhiteList.Contains(tooltipLine.Name))
                    continue;

                tooltips.Insert(i + 1, line);
                return;
            }
        }

        private static readonly HashSet<string> InsertDescriptionWhiteList = new()
        {
            "Material", "Consumable", "Ammo", "Placeable", "UseMana", "HealMana",
            "HealLife", "TileBoost", "HammerPower", "AxePower", "PickPower", "Defense",
            "Vanity", "Quest", "WandConsumes", "Equipable", "BaitPower", "NeedsBait",
            "FishingPower", "Knockback", "SpecialSpeedScaling", "NoSpeedScaling",
            "Speed", "CritChance", "Damage", "SocialDesc", "Social", "NoTransfer",
            "FavoriteDesc", "Favorite", "ItemName"
        };
    }
}