using SPYoyoMod.Content.Items.Mod.Weapons;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public class ItemLootBags : GlobalItem
    {
        public override void ModifyItemLoot(Item item, ItemLoot loot)
        {
            switch (item.type)
            {
                case ItemID.FloatingIslandFishingCrate:
                case ItemID.FloatingIslandFishingCrateHard:
                    loot.Add(ItemDropRule.NotScalingWithLuck(ModContent.ItemType<TheStellarThrowItem>(), TheStellarThrowItem.LootChanceDenominator));
                    break;
                default:
                    break;
            }
        }
    }
}