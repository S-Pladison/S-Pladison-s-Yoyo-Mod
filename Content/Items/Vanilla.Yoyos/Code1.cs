using SPYoyoMod.Utils;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class Code1Assets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Code1/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class Code1Item : VanillaYoyoBaseItem
    {
        public const int CritBonus = 16;

        public override int ItemType => ItemID.Code1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritBonus);

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(item, tooltips); //< Не удалять!

            var critLine = tooltips.Find(VanillaTooltipLine.CritChance);

            if (critLine is null)
                return;

            ItemUtils.ModifyFirstIntegerInLine(critLine, static (crit) =>
            {
                return crit; //< TODO: Модифицировать, если рядом есть враги
            });
        }
    }

    public sealed class Code1Projectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Code1;
    }
}