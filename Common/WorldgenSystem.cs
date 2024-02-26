using SPYoyoMod.Content.Items.Mod.Placeables;
using SPYoyoMod.Content.Items.Mod.Weapons;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace SPYoyoMod.Common
{
    public class WorldgenSystem : ModSystem
    {
        public enum ChestStyle : int
        {
            Skyware = 13
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            var index = tasks.FindIndex(genpass => genpass.Name.Equals("Dungeon"));

            if (index != -1) tasks.Insert(index + 1, new SpaceChestInDungeonGenPass());
        }

        public override void PostWorldGen()
        {
            var rand = WorldGen.genRand;

            foreach (var chest in Main.chest.Where(x => x is not null && WorldGen.InWorld(x.x, x.y, 16 * 2)))
            {
                var chestTile = Main.tile[chest.x, chest.y];

                if (chestTile.TileType != TileID.Containers) continue;

                var style = (ChestStyle)(chestTile.TileFrameX / 36);

                switch (style)
                {
                    case ChestStyle.Skyware:
                        {
                            if (rand.NextBool(TheStellarThrowItem.LootChanceDenominator) && TryInsertItemToFirstChestSlot<TheStellarThrowItem>(chest, out var item))
                            {
                                item.Prefix(-1);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private static bool TryInsertItemToFirstChestSlot<T>(Chest chest, out Item item) where T : ModItem
        {
            return TryInsertItemToFirstChestSlot(chest, ModContent.ItemType<T>(), out item);
        }

        private static bool TryInsertItemToFirstChestSlot(Chest chest, int itemType, out Item item)
        {
            if (chest.item.Count(i => i.type > ItemID.None) >= Chest.maxItems)
            {
                item = null;
                return false;
            }

            var items = chest.item.ToList();

            item = new();
            item.SetDefaults(itemType);

            items.Insert(0, item);
            items.Remove(items.Last());

            chest.item = items.ToArray();

            return true;
        }

        public class SpaceChestInDungeonGenPass : GenPass
        {
            public SpaceChestInDungeonGenPass() : base(Language.GetTextValue("Mods.SPYoyoMod.WorldGen.SpaceChest.Name"), 0.2f) { }

            protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
            {
                progress.Message = Language.GetTextValue("Mods.SPYoyoMod.WorldGen.SpaceChest.ProgressMessage");

                var flag = false;
                var attempt = 0;

                while (!flag && attempt < 15000)
                {
                    var randX = WorldGen.genRand.Next(GenVars.dMinX, GenVars.dMaxX);
                    int randY = WorldGen.genRand.Next((int)Main.worldSurface, GenVars.dMaxY);

                    if (!Main.wallDungeon[Main.tile[randX, randY].WallType] || Main.tile[randX, randY].HasTile)
                    {
                        attempt++;
                        continue;
                    }

                    flag = WorldGen.AddBuriedChest(randX, randY, ModContent.ItemType<BlackholeItem>(), false, 1, chestTileType: (ushort)ModContent.TileType<SpaceChestTile>());
                }
            }
        }
    }
}