using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Placeables
{
    public class TrappedSpaceChestItem : ModItem
    {
        public override string Texture => ModAssets.ItemsPath + "SpaceChest";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.TrapSigned[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<TrappedSpaceChestTile>());

            Item.width = 36;
            Item.height = 36;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 0, silver: 1, copper: 0);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<SpaceChestItem>()
                .AddIngredient(ItemID.Wire, 10)
                .AddTile(TileID.HeavyWorkBench)
                .Register();
        }
    }

    public class TrappedSpaceChestTile : TrappedChestTile
    {
        public override string Texture => ModAssets.TilesPath + "TrappedSpaceChest";
    }
}