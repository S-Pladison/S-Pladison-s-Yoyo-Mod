﻿using SPYoyoMod.Content.Items.Mod.Miscellaneous;
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

            foreach (var rule in globalLoot.Get())
            {
                if (rule is Conditions.YoyoCascade)
                {
                    globalLoot.Remove(rule);
                }
            }
        }
    }
}