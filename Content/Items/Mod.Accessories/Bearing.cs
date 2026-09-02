using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public sealed class BearingItem : ModItem
    {
        public const int PercentageStatBonus = 50;

        public override string Texture => $"{nameof(SPYoyoMod)}/Assets/Items/Mod.Accessories/Bearing_Item";
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(PercentageStatBonus);

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 36;
            Item.height = 34;

            Item.rare = ItemRarityID.White;
            Item.value = ItemUtils.SellPrice(platinum: 0, gold: 0, silver: 20, copper: 0);
        }
    }

    public sealed class BearingGlobalItem : GlobalItem
    {
        private static readonly HashSet<int> _itemTypeWithBearingEffectSet = [];

        public override void Load()
        {
            // Ищем все предметы, в дереве рецептов которых присутствует подшипник
            ModEvents.OnPostSetupRecipes += (recipes) =>
            {
                var stack = new Stack<int>();
                stack.Push(ModContent.ItemType<BearingItem>());

                while (stack.Count > 0)
                {
                    var itemType = stack.Pop();

                    if (!_itemTypeWithBearingEffectSet.Add(itemType))
                        continue;

                    for (var i = 0; i < recipes.Length; i++)
                    {
                        var recipe = recipes[i];
                        var type = recipe.createItem.type;

                        if (type > 0 && recipe.TryGetIngredient(itemType, out var _))
                            stack.Push(type);
                    }
                }
            };
        }

        public override void Unload()
        {
            _itemTypeWithBearingEffectSet.Clear();
        }

        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (_itemTypeWithBearingEffectSet.Contains(item.type))
                player.SetCustomFlagFor<BearingItem>();
        }
    }

    public sealed class BearingGlobalProjectile : GlobalProjectile, IModifyYoyoStatsProjectile
    {
        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
            => lateInstantiation && proj.IsYoyo() && !proj.IsCounterweight();

        public void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            if (!proj.TryGetOwner(out var owner) || !owner.HasCustomFlagFor<BearingItem>())
                return;

            statModifiers.LifeTime += BearingItem.PercentageStatBonus / 100.0f;
        }
    }
}