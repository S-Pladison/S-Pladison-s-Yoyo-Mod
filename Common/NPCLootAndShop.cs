using SPYoyoMod.Content.Items.Mod.Misc;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public class NPCLootAndShop : GlobalNPC
    {
        public override void ModifyGlobalLoot(GlobalLoot globalLoot)
        {
            globalLoot.Add(
                new ItemDropWithConditionRule(ModContent.ItemType<SpaceKey>(), 1250, 1, 1, new SpaceKeyCondition(), 1)
            );
        }

        public override void ModifyShop(NPCShop shop)
        {
            // ...
        }
    }
}