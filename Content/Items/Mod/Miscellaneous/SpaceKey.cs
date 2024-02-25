using SPYoyoMod.Common;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Miscellaneous
{
    public class SpaceKey : ModItem
    {
        public override string Texture => ModAssets.ItemsPath + "SpaceKey";

        public override void Load()
        {
            NPCEvents.OnModifyGlobalLoot += (globalLoot) =>
            {
                globalLoot.Add(
                    new ItemDropWithConditionRule(ModContent.ItemType<SpaceKey>(), 1250, 1, 1, new SpaceKeyCondition(), 1)
                );
            };
        }

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 26;

            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.sellPrice(platinum: 0, gold: 2, silver: 0, copper: 0);
            Item.maxStack = Item.CommonMaxStack;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var tooltip = tooltips.Find(i => i.Mod == "Terraria" && i.Name.StartsWith("Tooltip"));

            if (tooltip != null && !NPC.downedPlantBoss)
            {
                tooltip.Text = Language.GetTextValue("LegacyTooltip.59");
            }
        }
    }

    public class SpaceKeyCondition : IItemDropRuleCondition
    {
        public static LocalizedText Description { get; private set; }

        public SpaceKeyCondition()
        {
            Description ??= Language.GetOrRegister($"Mods.SPYoyoMod.DropConditions.{nameof(SpaceKeyCondition)}");
        }

        public bool CanDrop(DropAttemptInfo info)
        {
            return info.npc.value > 0f && Main.hardMode && !info.IsInSimulation && info.player.ZoneSkyHeight;
        }

        public bool CanShowItemDropInUI()
        {
            return true;
        }

        public string GetConditionDescription()
        {
            return Description.Value;
        }
    }
}