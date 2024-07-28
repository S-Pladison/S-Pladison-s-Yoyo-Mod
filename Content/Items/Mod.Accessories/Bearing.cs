using SPYoyoMod.Common.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public sealed class BearingItem : ModItem
    {
        public const int StatBonusInPercentage = 50;

        public override string Texture => $"{nameof(SPYoyoMod)}/Assets/Items/Mod.Accessories/Bearing_Item";
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(StatBonusInPercentage);

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 36;
            Item.height = 34;

            Item.rare = ItemRarityID.White;
            Item.value = ItemUtils.SellPrice(platinum: 0, gold: 0, silver: 20, copper: 0);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<BearingPlayer>().Equip();
        }
    }

    public sealed class BearingPlayer : ModPlayer
    {
        public bool Equipped { get; private set; }

        public override void ResetEffects()
        {
            Equipped = false;
        }

        public void Equip()
        {
            Equipped = true;
        }
    }

    public sealed class BearingGlobalProjectile : GlobalProjectile, IModifyYoyoStatsProjectile
    {
        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
            => lateInstantiation && proj.IsYoyo() && !proj.IsCounterweight();

        public void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            if (!proj.GetOwner().GetModPlayer<BearingPlayer>().Equipped)
                return;

            statModifiers.LifeTime += BearingItem.StatBonusInPercentage / 100.0f;
        }
    }
}