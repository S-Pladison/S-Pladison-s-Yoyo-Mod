using SPYoyoMod.Content.Items.Mod.Accessories;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public sealed class ItemRecipes : ModSystem
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

        public override void PostAddRecipes()
        {
            InsertBearingToYoyoBagRecipes();
        }

        private static void InsertBearingToYoyoBagRecipes()
        {
            for (var i = 0; i < Main.recipe.Length; i++)
            {
                ref var recipe = ref Main.recipe[i];

                if (!recipe.TryGetResult(ItemID.YoyoBag, out var _)) continue;
                if (!recipe.TryGetIngredient(ItemID.WhiteString, out var _)) continue;

                recipe.AddIngredient<BearingItem>();
            }
        }
    }
}