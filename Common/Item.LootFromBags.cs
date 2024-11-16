using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public sealed class LootFromBagsGlobalItem : GlobalItem
    {
        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            switch (item.type)
            {
                case ItemID.FloatingIslandFishingCrate:
                case ItemID.FloatingIslandFishingCrateHard:
                    itemLoot.Add(ItemDropRule.NotScalingWithLuck(ItemID.Terrarian, 5)); //< TODO: Заменить на Звездный бросок
                    break;
                default:
                    break;
            }
        }
    }
}