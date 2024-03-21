using SPYoyoMod.Content.Items.Mod.Placeables;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Miscellaneous
{
    public class SpaceKeyItem : ModItem
    {
        public class DropCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public static LocalizedText Description { get; private set; }

            public DropCondition()
            {
                Description ??= Language.GetOrRegister("Mods.SPYoyoMod.DropConditions.SpaceKeyCondition");
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

        public override string Texture => ModAssets.ItemsPath + "SpaceKey";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<SpaceChestItem>();
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
}