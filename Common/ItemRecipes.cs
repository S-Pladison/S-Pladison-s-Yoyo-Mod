using SPYoyoMod.Content.Items.Mod.Accessories;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public sealed class ItemRecipes : GlobalItem
    {
        public override void AddRecipes()
        {
            Recipe.Create(ItemID.Cascade)
                .AddIngredient(ItemID.HellstoneBar, 15)
                .AddTile(TileID.Anvils)
                .Register();

            Recipe.Create(ModContent.ItemType<BearingItem>())
                .AddRecipeGroup(RecipeGroupID.IronBar, 7)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}