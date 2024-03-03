using SPYoyoMod.Common;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public class BearingItem : ModItem
    {
        private static readonly HashSet<int> itemTypesWithBearingEffect = new();

        public override string Texture => ModAssets.ItemsPath + "Bearing";

        public override void Load()
        {
            SetEffectFlagForOtherModItems();
        }

        public override void Unload()
        {
            itemTypesWithBearingEffect.Clear();
        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 36;
            Item.height = 34;

            Item.rare = ItemRarityID.Blue;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }

        public override void AddRecipes()
        {
            var recipe = CreateRecipe();
            recipe.AddRecipeGroup(RecipeGroupID.IronBar, 7);
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.SetEffectFlag<BearingItem>();
        }

        private static void SetEffectFlagForOtherModItems()
        {
            // Find items that have a bearing in the recipe tree
            ModEvents.OnPostSetupRecipes += (recipes) =>
            {
                var tasks = new List<Task>();

                void FindItems(int itemType)
                {
                    for (var i = 0; i < recipes.Count; i++)
                    {
                        var recipe = Main.recipe[i];

                        if (recipe.TryGetIngredient(itemType, out var _) && !itemTypesWithBearingEffect.Contains(recipe.createItem.type))
                        {
                            itemTypesWithBearingEffect.Add(recipe.createItem.type);

                            tasks.Add(Task.Run(() => FindItems(recipe.createItem.type)));
                        }
                    }
                }

                FindItems(ModContent.ItemType<BearingItem>());

                Task.WaitAll(tasks.ToArray());
            };

            // Set effect flag for the found items
            // (Creating a separate GlobalItem class with overriding applicestoentity will not work)
            ItemEvents.OnUpdateEquip += (item, player) =>
            {
                if (!itemTypesWithBearingEffect.Contains(item.type)) return;

                player.SetEffectFlag<BearingItem>();
            };
        }
    }

    public class BearingGlobalProjectile : GlobalProjectile, IModifyYoyoStatsProjectile
    {
        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
        {
            return lateInstantiation && proj.IsYoyo() && !proj.IsCounterweight();
        }

        public void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            var owner = Main.player[proj.owner];

            if (!owner.GetEffectFlag<BearingItem>()) return;

            statModifiers.LifeTime += 0.5f;
        }
    }
}