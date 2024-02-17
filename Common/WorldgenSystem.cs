using SPYoyoMod.Content.Items.Mod.Placeables;
using SPYoyoMod.Content.Items.Mod.Weapons;
using System.Collections.Generic;
using Terraria;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace SPYoyoMod.Common
{
    public class WorldgenSystem : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            var index = tasks.FindIndex(genpass => genpass.Name.Equals("Dungeon"));

            if (index != -1) tasks.Insert(index + 1, new SpaceChestInDungeonGenPass());
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