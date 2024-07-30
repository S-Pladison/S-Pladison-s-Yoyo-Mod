using SPYoyoMod.Common.Hooks;
using SPYoyoMod.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<BearingPlayer>().Equip();
        }
    }

    public sealed class BearingPlayer : ModPlayer
    {
        public bool Equipped { get; private set; }

        public override void ResetEffects()
        {
            Equipped = false;
        }

        public void Equip()
        {
            Equipped = true;
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
                var tasks = new List<Task>();

                void FindItems(int itemType)
                {
                    for (var i = recipes.Length - 1; i > 0; i--)
                    {
                        var recipe = recipes[i];

                        if (recipe.TryGetIngredient(itemType, out var _) && !_itemTypeWithBearingEffectSet.Contains(recipe.createItem.type))
                        {
                            _itemTypeWithBearingEffectSet.Add(recipe.createItem.type);

                            tasks.Add(Task.Run(() => FindItems(recipe.createItem.type)));
                        }
                    }
                }

                FindItems(ModContent.ItemType<BearingItem>());

                Task.WaitAll(tasks.ToArray());
            };
        }

        public override void Unload()
        {
            _itemTypeWithBearingEffectSet.Clear();
        }

        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (!_itemTypeWithBearingEffectSet.Contains(item.type))
                return;

            player.GetModPlayer<BearingPlayer>().Equip();
        }
    }

    public sealed class BearingGlobalProjectile : GlobalProjectile, IModifyYoyoStatsProjectile
    {
        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
            => lateInstantiation && proj.IsYoyo() && !proj.IsCounterweight();

        public void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            if (!proj.GetOwner().GetModPlayer<BearingPlayer>().Equipped)
                return;

            statModifiers.LifeTime += BearingItem.PercentageStatBonus / 100.0f;
        }
    }
}