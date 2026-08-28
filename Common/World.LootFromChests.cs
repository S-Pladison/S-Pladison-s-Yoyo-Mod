using SPYoyoMod.Content.Items.Mod.Yoyos;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public sealed class LoolFromChestsSystem : ModSystem
    {
        public readonly struct ChestItemInfo(int itemType, ChestStyle chestStyle, float chance)
        {
            public readonly int ItemType = itemType;
            public readonly ChestStyle ChestStyle = chestStyle;
            public readonly float Chance = Math.Clamp(chance, 0f, 1f);
        }

        public static readonly List<ChestItemInfo> LootFromChests = [];

        private Dictionary<ChestStyle, List<ChestItemInfo>> _lootFromChestsByChestStyle;

        public override void PostSetupContent()
        {
            LootFromChests.Clear();
            LootFromChests.Add(new(TheStellarThrowItem.Type, ChestStyle.Skyware, 0.15f));

            _lootFromChestsByChestStyle = LootFromChests
                .GroupBy(loot => loot.ChestStyle)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                );
        }

        public override void Unload()
        {
            LootFromChests.Clear();
            _lootFromChestsByChestStyle?.Clear();
        }

        public override void PostWorldGen()
        {
            ModLogger.Info("Starting to populate chests with modded loot...");

            // Получаем список сундуков, расположенных в случайном порядке, сгрупированные по типу стиля сундука
            var chestsByStyle = Main.chest
                .Where(c => c is not null && Framing.GetTileSafely(c.x, c.y) is Tile tile && tile.HasTile && (tile.TileType == TileID.Containers || tile.TileType == TileID.Containers2))
                .GroupBy(c =>
                {
                    var tile = Main.tile[c.x, c.y];
                    var style = (ChestStyle)(tile.TileFrameX / 36);

                    return Enum.IsDefined<ChestStyle>(style) ? style : ChestStyle.Undefined;
                })
                .Where(g => _lootFromChestsByChestStyle.ContainsKey(g.Key))
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (style, chests) in chestsByStyle)
                ModLogger.Info($"Found {chests.Count} chests of style [{style}:{(int)style}]");

            foreach (var (style, chests) in chestsByStyle)
            {
                foreach (var loot in _lootFromChestsByChestStyle[style])
                {
                    bool hasGuaranteedItem = false;

                    // Сначала пытаемся добавить предменты с некоторой вероятностью в каждый сундук
                    foreach (var chest in chests)
                    {
                        if (WorldGen.genRand.NextFloat() > loot.Chance)
                            continue;

                        if (!TryInsertItemToFirstChestSlot(chest, loot.ItemType, out _))
                            continue;

                        ModLogger.Info($"Inserted item [Type:{loot.ItemType}] [Name:{ContentSamples.ItemsByType[loot.ItemType].Name}] into chest at [Style:{style}:{(int)style}] [Coord:{chest.x},{chest.y}]");
                        hasGuaranteedItem = true;
                    }

                    if (hasGuaranteedItem)
                        continue;

                    // Пытаемся добавить гарантированный предмет в первый попавшийся сундук
                    foreach (var chest in chests.OrderBy(_ => WorldGen.genRand.NextFloat()))
                    {
                        if (TryInsertItemToFirstChestSlot(chest, loot.ItemType, out _))
                        {
                            ModLogger.Info($"Inserted guaranteed item [Type:{loot.ItemType}] [Name:{ContentSamples.ItemsByType[loot.ItemType].Name}] into chest at [Style:{style}:{(int)style}] [Coord:{chest.x},{chest.y}]");
                            hasGuaranteedItem = true;
                            break;
                        }
                    }

                    if (!hasGuaranteedItem)
                        ModLogger.Info($"Failed to insert guaranteed item [Type:{loot.ItemType}] [Name:{ContentSamples.ItemsByType[loot.ItemType].Name}] into any chest of style [Style:{style}:{(int)style}]...");
                }
            }

            ModLogger.Info("Finished populating chests with modded loot");
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
