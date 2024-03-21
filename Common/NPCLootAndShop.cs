using SPYoyoMod.Content.Items.Mod.Accessories;
using SPYoyoMod.Content.Items.Mod.Miscellaneous;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public class NPCLootAndShop : GlobalNPC
    {
        public override void ModifyGlobalLoot(GlobalLoot globalLoot)
        {
            globalLoot.Add(
                new ItemDropWithConditionRule(ModContent.ItemType<SpaceKeyItem>(), 1250, 1, 1, new SpaceKeyItem.DropCondition(), 1)
            );

            foreach (var rule in globalLoot.Get())
            {
                if (rule is Conditions.YoyoCascade)
                {
                    globalLoot.Remove(rule);
                }
            }
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            switch (npc.type)
            {
                case NPCID.AngryNimbus:
                    npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<EmbraceOfRainItem>(), 50));
                    break;
                default:
                    break;
            }
        }
    }
}