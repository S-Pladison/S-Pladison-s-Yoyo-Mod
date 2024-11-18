using SPYoyoMod.Content.Items.Mod.Accessories;
using SPYoyoMod.Content.Items.Mod.Yoyos;
using SPYoyoMod.Core;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    [LoadPriority(sbyte.MaxValue)]
    public sealed class ItemRecipes : ModSystem
    {
        public override void AddRecipes()
        {
            AddYoyoRecipes();
            AddAccessoryRecipes();
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

            Recipe.Create(ModContent.ItemType<TitaniumYoyoItem>())
                .AddIngredient(ItemID.TitaniumBar, 13)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        private static void AddAccessoryRecipes()
        {
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