using System;
using System.Collections.Generic;
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
            if (ItemID.Sets.Yoyo[item.type]) return true;
            if (item.shoot <= ProjectileID.None) return false;

            var dict = ContentSamples.ProjectilesByType;

            if (dict.TryGetValue(item.shoot, out Projectile proj))
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

        /// <summary>
        /// Вставляет строку в позицию, где обычно находится описание предмета.
        /// </summary>
        public static void InsertDescription(this List<TooltipLine> tooltips, TooltipLine line)
            => tooltips.InsertDescription([line]);

        /// <summary>
        /// Вставляет строки в позицию, где обычно находится описание предмета.
        /// </summary>
        public static void InsertDescription(this List<TooltipLine> tooltips, IList<TooltipLine> lines)
        {
            for (var i = tooltips.Count - 1; i >= 0; i--)
            {
                var tooltipLine = tooltips[i];

                if (tooltipLine.Mod != "Terraria")
                    continue;

                if (!tooltipLine.Name.StartsWith("Tooltip") && !_descriptionWhitelistSet.Contains(tooltipLine.Name))
                    continue;

                for (var j = 0; j < lines.Count; j++)
                    tooltips.Insert(i + j + 1, lines[j]);

                return;
            }
        }
        
        private static readonly HashSet<string> _descriptionWhitelistSet =
        [
            "Material", "Consumable", "Ammo", "Placeable", "UseMana", "HealMana",
            "HealLife", "TileBoost", "HammerPower", "AxePower", "PickPower", "Defense",
            "Vanity", "Quest", "WandConsumes", "Equipable", "BaitPower", "NeedsBait",
            "FishingPower", "Knockback", "SpecialSpeedScaling", "NoSpeedScaling",
            "Speed", "CritChance", "Damage", "SocialDesc", "Social", "NoTransfer",
            "FavoriteDesc", "Favorite", "ItemName"
        ];
    }
}