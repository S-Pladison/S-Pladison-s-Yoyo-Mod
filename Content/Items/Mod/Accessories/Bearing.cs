using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Utils.DataStructures;
using SPYoyoMod.Utils.Extensions;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public class Bearing : ModItem
    {
        public override string Texture { get => ModAssets.ItemsPath + nameof(Bearing); }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 40;
            Item.height = 36;

            Item.rare = ItemRarityID.Blue;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    public class BearingGlobalProjectile : GlobalProjectile, IModifyYoyoStatsProjectile
    {
        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation) { return proj.IsYoyo(); }

        public void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            var owner = Main.player[proj.owner];

            if (!owner.HasEquipped(ModContent.ItemType<Bearing>())) return;

            statModifiers.LifeTime += 0.5f;
        }
    }
}