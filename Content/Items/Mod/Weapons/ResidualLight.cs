using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    [Autoload(false)]
    public class ResidualLightItem : YoyoItem
    {
        public override string Texture => ModAssets.ItemsPath + "ResidualLight";
        public override int GamepadExtraRange => 15;

        public override void YoyoSetDefaults()
        {
            Item.damage = 20;
            Item.knockBack = 2.5f;

            Item.shoot = ModContent.ProjectileType<ResidualLightProjectile>();

            Item.rare = ItemRarityID.Pink;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Chik)
                .AddIngredient(ItemID.HallowedBar, 12)
                .AddIngredient(ItemID.LightShard, 2)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public class ResidualLightProjectile : YoyoProjectile
    {
        public override string Texture => ModAssets.ProjectilesPath + "ResidualLight";
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;
    }
}