using SPYoyoMod.Common.Configs;
using SPYoyoMod.Utils;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items
{
    public abstract class VanillaYoyoItem : GlobalItem, ILocalizedModType
    {
        private readonly int yoyoType;

        public VanillaYoyoItem(int yoyoType)
        {
            this.yoyoType = yoyoType;
        }

        public sealed override bool AppliesToEntity(Item entity, bool lateInstantiation) { return entity.type < ItemID.Count && entity.type.Equals(yoyoType); }
        public sealed override bool IsLoadingEnabled(Terraria.ModLoader.Mod mod) { return ModContent.GetInstance<ServerSideConfig>().ReworkedVanillaYoyos; }

        public sealed override void SetStaticDefaults()
        {
            _ = Tooltip;

            YoyoSetStaticDefaults();
        }

        public sealed override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (Tooltip.Value is not null && Tooltip.Value != "")
            {
                var tooltipLine = new TooltipLine(Mod, "ModTooltip", Tooltip.Value);
                var tooltipLines = TooltipUtils.Split(tooltipLine, '\n');

                TooltipUtils.InsertDescriptions(tooltips, tooltipLines);
            }

            YoyoModifyTooltips(item, tooltips);
        }

        public virtual string LocalizationCategory => "VanillaItems";
        public virtual LocalizedText Tooltip => this.GetLocalization("Tooltip", () => "");
        public virtual void YoyoModifyTooltips(Item item, List<TooltipLine> tooltips) { }
        public virtual void YoyoSetStaticDefaults() { }
    }
}