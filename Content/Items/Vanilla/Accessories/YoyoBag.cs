﻿using SPYoyoMod.Content.Items.Mod.Accessories;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Accessories
{
    public class YoyoBagItem : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return item.type.Equals(ItemID.YoyoBag);
        }

        public override void Load()
        {
            ModEvents.OnPostAddRecipes += InsertBearingToRecipes;
        }

        private static void InsertBearingToRecipes(Recipe[] recipes)
        {
            for (var i = 0; i < recipes.Length; i++)
            {
                ref var recipe = ref Main.recipe[i];

                if (!recipe.TryGetResult(ItemID.YoyoBag, out var _)) continue;
                if (!recipe.TryGetIngredient(ItemID.WhiteString, out var _)) continue;

                recipe.AddIngredient<BearingItem>();
            }
        }
    }
}