using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils.Rendering
{
    public static class TooltipUtils
    {
        public static TooltipLine[] Split(TooltipLine line, params char[] separator)
        {
            if (!ModLoader.TryGetMod(line.Mod, out var mod))
                throw new Exception($"Failed to find mod {line.Mod}...");

            var split = line.Text.Split(separator);
            var lines = new TooltipLine[split.Length];

            for (var i = 0; i < split.Length; i++)
            {
                lines[i] = new(mod, line.Name + i.ToString(), split[i])
                {
                    IsModifier = line.IsModifier,
                    IsModifierBad = line.IsModifierBad,
                    OverrideColor = line.OverrideColor
                };
            }

            return lines;
        }

        public static TooltipLine FindDescriptionLast(List<TooltipLine> tooltips)
        {
            return tooltips.FindLast(i => i.Mod == "Terraria" && i.Name.StartsWith("Tooltip"));
        }

        public static int FindDescriptionLastIndex(List<TooltipLine> tooltips)
        {
            return tooltips.FindLastIndex(i => i.Mod == "Terraria" && i.Name.StartsWith("Tooltip"));
        }

        public static void InsertDescription(List<TooltipLine> tooltips, TooltipLine line)
        {
            for (var i = tooltips.Count - 1; i >= 0; i--)
            {
                var tooltipLine = tooltips[i];

                if (tooltipLine.Mod != "Terraria")
                    continue;

                if (!tooltipLine.Name.StartsWith("Tooltip")
                    && !insertDescriptionWhitelist.Contains(tooltipLine.Name))
                    continue;

                tooltips.Insert(i + 1, line);
                return;
            }
        }

        public static void InsertDescription(List<TooltipLine> tooltips, IList<TooltipLine> lines)
        {
            for (var i = tooltips.Count - 1; i >= 0; i--)
            {
                var tooltipLine = tooltips[i];

                if (tooltipLine.Mod != "Terraria")
                    continue;

                if (!tooltipLine.Name.StartsWith("Tooltip")
                    && !insertDescriptionWhitelist.Contains(tooltipLine.Name))
                    continue;

                for (var j = 0; j < lines.Count; j++)
                    tooltips.Insert(i + j + 1, lines[j]);

                return;
            }
        }

        public static void ModifyWeaponDamage(List<TooltipLine> tooltips, Func<int, int> func)
        {
            var damageLine = tooltips.FirstOrDefault(x => x.Mod == "Terraria" && x.Name == "Damage");

            if (damageLine is null)
                return;

            ModifyFirstIntegerInLine(damageLine, func);
        }

        public static void ModifyWeaponCrit(List<TooltipLine> tooltips, Func<int, int> func)
        {
            var critLine = tooltips.FirstOrDefault(x => x.Mod == "Terraria" && x.Name == "CritChance");

            if (critLine is null)
                return;

            ModifyFirstIntegerInLine(critLine, func);
        }

        private static void ModifyFirstIntegerInLine(TooltipLine line, Func<int, int> func)
        {
            var split = line.Text.Split(' ');

            if (split.Length == 0)
                return;

            for (int i = 0; i < split.Length; i++)
            {
                ref var str = ref split[i];

                if (int.TryParse(str, out int @int))
                {
                    str = $"{func(@int)}";
                    line.Text = string.Join(' ', split);
                    return;
                }

                if (str.EndsWith("%") && int.TryParse(str.Replace("%", ""), out @int))
                {
                    str = $"{func(@int)}%";
                    line.Text = string.Join(' ', split);
                    return;
                }
            }
        }

        private static readonly HashSet<string> insertDescriptionWhitelist = new()
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