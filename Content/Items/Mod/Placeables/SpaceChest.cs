using SPYoyoMod.Content.Items.Mod.Miscellaneous;
using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Placeables
{
    public class SpaceChestItem : ModItem
    {
        public override string Texture => ModAssets.ItemsPath + "SpaceChest";

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<SpaceChestTile>());

            Item.width = 32;
            Item.height = 32;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 0, silver: 5, copper: 0);
        }
    }

    public class SpaceChestTile : ChestTile
    {
        public override string Texture => ModAssets.TilesPath + "SpaceChest";
        public override int PlaceItemType => ModContent.ItemType<SpaceChestItem>();
        public override int KeyItemType => ModContent.ItemType<SpaceKey>();

        public override void ChestSetStaticDefaults()
        {
            DustType = ModContent.DustType<SpaceChestDust>();
        }

        public override bool UnlockChest(int i, int j, ref short frameXAdjustment, ref int dustType, ref bool manual)
        {
            if (!NPC.downedPlantBoss) return false;

            AchievementsHelper.NotifyProgressionEvent(AchievementHelperID.Events.UnlockedBiomeChest);

            return base.UnlockChest(i, j, ref frameXAdjustment, ref dustType, ref manual);
        }
    }

    public class SpaceChestDust : ModDust
    {
        public override string Texture => ModAssets.DustsPath + "SpaceChest";
    }
}