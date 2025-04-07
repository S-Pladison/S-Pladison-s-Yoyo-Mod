using SPYoyoMod.Content.Items.Mod.Yoyos;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public sealed class LoolFromChestsSystem : ModSystem
    {
        /// <summary>
        /// Данные об единице лута генерируемого сундука.
        /// </summary>
        public readonly struct ChestItemInfo(int itemType, ChestStyle chestStyle, float chance)
        {
            public readonly int ItemType = itemType;
            public readonly ChestStyle ChestStyle = chestStyle;
            public readonly float Chance = Math.Clamp(chance, 0f, 1f);
        }

        /// <summary>
        /// Импровизированная база данных лута генерируемых в мире сундуков.
        /// </summary>
        public static readonly List<ChestItemInfo> LootFromChests =
        [
            new(ModContent.ItemType<TheStellarThrowItem>(), ChestStyle.Skyware, 0.15f)
        ];

        /// <summary>
        /// База данных лута, доступ к данным которых производится по типу сундука.
        /// </summary>
        private Dictionary<ChestStyle, List<ChestItemInfo>> _lootFromChestsByChestStyle;

        public override void PostSetupContent()
        {
            _lootFromChestsByChestStyle = LootFromChests
                .GroupBy(loot => loot.ChestStyle)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                );
        }

        public override void Unload()
        {
            _lootFromChestsByChestStyle?.Clear();
        }

        public override void PostWorldGen()
        {
            // Набор лута, который хотя бы раз был помещен в сундук
            var successfulItemSet = new HashSet<ChestItemInfo>();

            // Получаем список сундуков, расположенных в случайном порядке
            var chests = Main.chest
                .Where(c => c is not null && Framing.GetTileSafely(c.x, c.y) is Tile tile && tile.HasTile)
                .OrderBy(_ => WorldGen.genRand.Next());

            foreach (var chest in chests)
            {
                var style = (ChestStyle)(Main.tile[chest.x, chest.y].TileFrameX / 36);

                if (!_lootFromChestsByChestStyle.ContainsKey(style))
                    continue;

                var lootCollection = _lootFromChestsByChestStyle[style];

                foreach (var loot in lootCollection)
                {
                    if (successfulItemSet.Contains(loot) && WorldGen.genRand.NextFloat() > loot.Chance)
                        continue;

                    if (!TryInsertItemToFirstChestSlot(chest, loot.ItemType, out _))
                        continue;

                    successfulItemSet.Add(loot);
                }
            }
        }

        private static bool TryInsertItemToFirstChestSlot(Chest chest, int itemType, out Item item)
        {
            item = null;

            ref var inventory = ref chest.item;
            var slot = -1;

            // Ищем свободный слот
            for (var i = 0; i < inventory.Length; i++)
            {
                if (inventory[i].IsAir)
                {
                    slot = i;
                    break;
                }
            }

            // Свободных слотов под наш предмет нет, завершаем функцию...
            if (slot == -1)
                return false;

            // Перемещаем все предметы вправо, чтобы освободить место под наш предмет
            for (var i = slot; i > 0; i--)
                (inventory[i - 1], inventory[i]) = (inventory[i], inventory[i - 1]);

            // Освободившийся первый слот теперь является нашим предметом
            item = inventory[0];
            item.SetDefaults(itemType);
            item.Prefix(-1);

            return true;
        }
    }
}
