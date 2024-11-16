using SPYoyoMod.Utils;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public sealed class LoolFromChestsSystem : ModSystem
    {
        public override void PostWorldGen()
        {
            for (int chestIndex = 0; chestIndex < 1000; chestIndex++)
            {
                var chest = Main.chest[chestIndex];

                if (!(chest is not null && Framing.GetTileSafely(chest.x, chest.y) is Tile tile && tile.HasTile))
                    continue;

                var style = (ChestStyle)(tile.TileFrameX / 36);

                AddLootToChest(chest, style);
            }
        }

        private static void AddLootToChest(Chest chest, ChestStyle style)
        {
            // TODO: Реализовать всё так, чтобы была возможность гарантированно помещать 1 предмет в мире
        }
    }
}
