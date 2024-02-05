using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Utils.DataStructures;
using SPYoyoMod.Utils.Extensions;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public class BearingItem : ModItem
    {
        public override string Texture { get => ModAssets.ItemsPath + "Bearing"; }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 36;
            Item.height = 34;

            Item.rare = ItemRarityID.Blue;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }

        public override void AddRecipes()
        {
            var recipe = CreateRecipe();
            recipe.AddRecipeGroup(RecipeGroupID.IronBar, 7);
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.SetEffectFlag<BearingItem>();
        }
    }

    public class BearingGlobalProjectile : GlobalProjectile, IModifyYoyoStatsProjectile
    {
        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation) { return proj.IsYoyo(); }

        public void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            var owner = Main.player[proj.owner];

            if (!owner.GetEffectFlag<BearingItem>()) return;

            statModifiers.LifeTime += 0.5f;
        }
    }
}