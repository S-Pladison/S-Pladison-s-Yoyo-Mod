using SPYoyoMod.Core;
using SPYoyoMod.Utils;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Yoyos
{
    public abstract partial class YoyoItem
    {
        [LoadBefore(typeof(YoyoItem))]
        private sealed class OverrideGlobalItem : GlobalItem
        {
            public override bool AppliesToEntity(Item item, bool lateInstantiation)
            {
                if (!lateInstantiation)
                    return false;

                return TryGet(item.type, out var definition) && definition.IsOverride;
            }

            public override void SetStaticDefaults()
            {
                foreach (var definition in ModContent.GetContent<YoyoItem>())
                {
                    if (!definition.IsOverride)
                        continue;

                    if (definition.GamepadExtraRange.HasValue)
                        ItemID.Sets.GamepadExtraRange[definition.Type] = definition.GamepadExtraRange.Value;

                    _ = definition.Tooltip;
                }
            }

            public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
            {
                if (!TryGet(item.type, out var definition))
                    return;

                var value = definition.Tooltip.Value;

                if (value is null || value == "")
                    return;

                var tooltipLine = new TooltipLine(definition.Mod, "ModTooltip", value);
                tooltips.InsertDescription(tooltipLine.Split('\n'));
            }
        }

        [Autoload(false)]
        private sealed class ModItemStub<T> : ModItem where T : YoyoItem
        {
            private static T Definition => Get<T>();

            public override string Name => typeof(T).Name;
            public override string Texture => Definition.Texture;
            public override LocalizedText Tooltip => Definition.Tooltip;

            public override void SetStaticDefaults()
            {
                ItemID.Sets.Yoyo[Type] = true;
                ItemID.Sets.GamepadExtraRange[Type] = Definition.GamepadExtraRange.Value;
                ItemID.Sets.GamepadSmartQuickReach[Type] = true;
            }

            public override void SetDefaults()
            {
                Item.DamageType = DamageClass.MeleeNoSpeed;
                Item.damage = 1;
                Item.width = 30;
                Item.height = 26;
                Item.shootSpeed = 16f;

                Item.UseSound = SoundID.Item1;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.useAnimation = 25;
                Item.useTime = 25;

                Item.channel = true;
                Item.noMelee = true;
                Item.noUseGraphic = true;
                Item.shoot = YoyoProjectile.Get(Definition.ProjectileClass).Type;
            }
        }
    }
}
