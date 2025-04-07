using SPYoyoMod.Content.Items.Mod.Accessories;
using SPYoyoMod.Content.Items.Mod.Miscellaneous;
using SPYoyoMod.Content.Items.Mod.Yoyos;
using SPYoyoMod.Core;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    [LoadPriority(sbyte.MaxValue)]
    public sealed class ItemRecipesSystem : ModSystem
    {
        public override void AddRecipes()
        {
            AddYoyoRecipes();
            AddAccessoryRecipes();
            AddOtherRecipes();
        }

        private static void AddYoyoRecipes()
        {
            Recipe.Create(ItemID.Cascade)
                .AddIngredient(ItemID.HellstoneBar, 15)
                .AddTile(TileID.Anvils)
                .Register();

            Recipe.Create(ModContent.ItemType<BellowingThunderItem>())
                .AddIngredient(ItemID.CorruptYoyo)
                .AddIngredient(ItemID.Valor)
                .AddIngredient(ItemID.JungleYoyo)
                .AddIngredient(ItemID.Cascade)
                .AddTile(TileID.DemonAltar)
                .Register();

            Recipe.Create(ModContent.ItemType<BellowingThunderItem>())
                .AddIngredient(ItemID.CrimsonYoyo)
                .AddIngredient(ItemID.Valor)
                .AddIngredient(ItemID.JungleYoyo)
                .AddIngredient(ItemID.Cascade)
                .AddTile(TileID.DemonAltar)
                .Register();

            Recipe.Create(ItemID.Gradient)
                .AddIngredient(ItemID.CobaltBar, 5)
                .AddIngredient(ItemID.GoldBar, 10)
                .AddIngredient(ItemID.Marble, 25)
                .AddTile(TileID.Anvils)
                .Register();

            Recipe.Create(ItemID.Gradient)
                .AddIngredient(ItemID.PalladiumBar, 5)
                .AddIngredient(ItemID.GoldBar, 10)
                .AddIngredient(ItemID.Marble, 25)
                .AddTile(TileID.Anvils)
                .Register();
        }

        private static void AddAccessoryRecipes()
        {
            Recipe.Create(ModContent.ItemType<BearingItem>())
                .AddRecipeGroup(RecipeGroupID.IronBar, 7)
                .AddTile(TileID.Anvils)
                .Register();
        }

        private static void AddOtherRecipes()
        {
            Recipe.Create(ModContent.ItemType<StrangeDrinkItem>())
                .AddIngredient(ItemID.LifeFruit, 3)
                .AddIngredient(ItemID.Milkshake)
                .AddIngredient(ItemID.GrapeJuice)
                .AddIngredient(ItemID.PrismaticPunch)
                .AddIngredient(ItemID.PinaColada)
                .AddIngredient(ItemID.TropicalSmoothie)
                .AddIngredient(ItemID.SmoothieofDarkness)
                .AddIngredient(ItemID.AppleJuice)
                .AddIngredient(ItemID.BananaDaiquiri)
                .AddIngredient(ItemID.Lemonade)
                .AddIngredient(ItemID.PeachSangria)
                .AddTile(TileID.CookingPots)
                .Register();

            Recipe.Create(ModContent.ItemType<StrangeDrinkItem>())
                .AddIngredient(ItemID.LifeFruit, 3)
                .AddIngredient(ItemID.Milkshake)
                .AddIngredient(ItemID.GrapeJuice)
                .AddIngredient(ItemID.PrismaticPunch)
                .AddIngredient(ItemID.PinaColada)
                .AddIngredient(ItemID.TropicalSmoothie)
                .AddIngredient(ItemID.BloodyMoscato)
                .AddIngredient(ItemID.AppleJuice)
                .AddIngredient(ItemID.BananaDaiquiri)
                .AddIngredient(ItemID.Lemonade)
                .AddIngredient(ItemID.PeachSangria)
                .AddTile(TileID.CookingPots)
                .Register();
        }

        public override void PostAddRecipes()
        {
            RemoveWoodenYoyoFromChikRecipe();
            InsertBearingToYoyoBagRecipes();
        }

        private static void RemoveWoodenYoyoFromChikRecipe()
        {
            for (var i = 0; i < Main.recipe.Length; i++)
            {
                ref var recipe = ref Main.recipe[i];

                if (!recipe.TryGetResult(ItemID.Chik, out var _)) continue;

                recipe.RemoveIngredient(ItemID.WoodYoyo);
            }
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