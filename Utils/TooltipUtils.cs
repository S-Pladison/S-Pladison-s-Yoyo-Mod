using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    public static class TooltipUtils
    {
        public static TooltipLine[] Split(TooltipLine line, params char[] separator)
        {
            if (!ModLoader.TryGetMod(line.Mod, out Mod mod))
                throw new Exception($"Failed to find mod {line.Mod}...");

            var split = line.Text.Split(separator);
            var lines = new TooltipLine[split.Length];

            for (int i = 0; i < split.Length; i++)
            {
                lines[i] = new(mod, line.Name + i.ToString(), split[i]);
                lines[i].IsModifier = line.IsModifier;
                lines[i].IsModifierBad = line.IsModifierBad;
                lines[i].OverrideColor = line.OverrideColor;
            }

            return lines;
        }

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

        public static void InsertDescriptions(List<TooltipLine> tooltips, IList<TooltipLine> lines)
        {
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                var tooltipLine = tooltips[i];

                if (tooltipLine.Mod != "Terraria")
                    continue;

                if (!tooltipLine.Name.StartsWith("Tooltip")
                    && !InsertDescriptionWhiteList.Contains(tooltipLine.Name))
                    continue;

                for (int j = 0; j < lines.Count; j++)
                    tooltips.Insert(i + j + 1, lines[j]);

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