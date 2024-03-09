using SPYoyoMod.Common.Configs;
using SPYoyoMod.Utils.Rendering;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items
{
    public abstract class VanillaYoyoItem : GlobalItem, ILocalizedModType
    {
        /// <summary>
        /// The Item ID of this yoyo. The Item ID is a unique number assigned
        /// to each Item loaded into the game.
        /// </summary>
        public abstract int YoyoType { get; }

        public virtual string LocalizationCategory => "VanillaItems";
        public virtual LocalizedText Tooltip => this.GetLocalization("Tooltip", () => "");

        public sealed override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type < ItemID.Count && entity.type.Equals(YoyoType);
        }

        public sealed override bool IsLoadingEnabled(Terraria.ModLoader.Mod mod)
        {
            return ModContent.GetInstance<ServerSideConfig>().ReworkedVanillaYoyos;
        }

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

        /// <inheritdoc cref="SetStaticDefaults" />
        public virtual void YoyoSetStaticDefaults() { }

        /// <inheritdoc cref="ModifyTooltips(Item, List{TooltipLine})" />
        public virtual void YoyoModifyTooltips(Item item, List<TooltipLine> tooltips) { }
    }
}