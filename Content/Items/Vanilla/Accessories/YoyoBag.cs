using SPYoyoMod.Common;
using SPYoyoMod.Content.Items.Mod.Accessories;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Accessories
{
    public class YoyoBagItem : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation) { return item.type.Equals(ItemID.YoyoBag); }

        public override void Load()
        {
            ModEvents.OnPostSetupRecipes += InsertBearingToRecipes;
        }

        public override void UpdateEquip(Item item, Player player)
        {
            player.GetModPlayer<PlayerEffectFlags>().SetFlag<BearingItem>();
        }

        private static void InsertBearingToRecipes()
        {
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                ref var recipe = ref Main.recipe[i];

                if (!recipe.TryGetResult(ItemID.YoyoBag, out Item _)) continue;
                if (!recipe.TryGetIngredient(ItemID.WhiteString, out Item _)) continue;

                for (int counterweightType = ItemID.BlackCounterweight; counterweightType <= ItemID.YellowCounterweight; counterweightType++)
                {
                    if (!recipe.TryGetIngredient(counterweightType, out Item _)) continue;

                    recipe.AddIngredient<BearingItem>();
                }
            }
        }
    }
}