using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    public static class ItemUtils
    {
        /// <summary>
        /// Является ли этот предмет оружием типа йо-йо.
        /// </summary>
        public static bool IsYoyo(this Item item)
        {
            if (ItemID.Sets.Yoyo[item.type])
                return true;

            if (item.shoot <= ProjectileID.None)
                return false;

            if (ContentSamples.ProjectilesByType.TryGetValue(item.shoot, out Projectile proj))
                return proj.IsYoyo();

            proj = ProjectileLoader.GetProjectile(item.shoot)?.Projectile;

            if (proj is not null)
                return proj.IsYoyo();

            proj = new Projectile();
            proj.SetDefaults(item.shoot);

            return proj.IsYoyo();
        }

        /// <summary>
        /// Конвертирует предоставленную стоимость продажи в медные монеты. Это значение в пять раз превышает <see cref="Item.buyPrice"/>.<br/>
        /// Если присвоено <see cref="Item.value"/>, то предмет будет продан за указанную стоимость.
        /// </summary>
        /// <returns>Преобразованное значение.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SellPrice(int platinum = 0, int gold = 0, int silver = 0, int copper = 0)
            => Item.sellPrice(platinum, gold, silver, copper);

        /// <summary>
        /// Разбивает строку на массив подстрок. Принимает массив символов, которые и будут служить разделителями.
        /// </summary>
        public static TooltipLine[] Split(this TooltipLine line, params char[] separator)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
            extern static TooltipLine TooltipLineCtor(string mod, string name, string text);

            var split = line.Text.Split(separator);
            var lines = new TooltipLine[split.Length];

            for (var i = 0; i < split.Length; i++)
            {
                ref var tooltipLine = ref lines[i];
                tooltipLine = TooltipLineCtor(line.Mod, line.Name + i.ToString(), split[i]);

                tooltipLine.IsModifier = line.IsModifier;
                tooltipLine.IsModifierBad = line.IsModifierBad;
                tooltipLine.OverrideColor = line.OverrideColor;
            }

            return lines;
        }

        /// <summary>
        /// Вставляет строку в позицию, где обычно находится описание предмета.
        /// </summary>
        public static void InsertDescription(this IList<TooltipLine> tooltips, TooltipLine line)
            => tooltips.InsertDescription([line]);

        /// <summary>
        /// Вставляет строки в позицию, где обычно находится описание предмета.
        /// </summary>
        public static void InsertDescription(this IList<TooltipLine> tooltips, IList<TooltipLine> lines)
        {
            for (var i = tooltips.Count - 1; i >= 0; i--)
            {
                var tooltipLine = tooltips.ElementAt(i);
                var tooltipName = tooltipLine.Name;

                if (tooltipLine.Mod != "Terraria")
                    continue;

                if (tooltipName.StartsWith("Tooltip"))
                    tooltipName = "Tooltip";

                if (!Enum.TryParse<VanillaTooltipLine>(tooltipName, out var vanillaTooltipLine) || vanillaTooltipLine == VanillaTooltipLine.Undefined)
                    continue;

                if (!_descriptionWhitelistSet.Contains(vanillaTooltipLine))
                    continue;

                for (var j = 0; j < lines.Count; j++)
                    tooltips.Insert(i + j + 1, lines[j]);

                return;
            }
        }

        /// <summary>
        /// Ищет ванильную строку всплывающей подсказки.
        /// </summary>
        public static TooltipLine Find(this IReadOnlyCollection<TooltipLine> tooltips, VanillaTooltipLine line)
        {
            if (line == VanillaTooltipLine.Undefined)
                return null;

            return tooltips.FirstOrDefault(x => x.Mod == "Terraria" && x.Name == line.ToString());
        }

        /// <summary>
        /// Изменяет первое целочисленное значение, найденное в строке.
        /// </summary>
        public static void ModifyFirstIntegerInLine(TooltipLine line, Func<int, int> func)
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

        private static readonly HashSet<VanillaTooltipLine> _descriptionWhitelistSet = new(
            Enumerable.Range((int)VanillaTooltipLine.ItemName, (int)VanillaTooltipLine.Tooltip).Cast<VanillaTooltipLine>()
        );
    }
}