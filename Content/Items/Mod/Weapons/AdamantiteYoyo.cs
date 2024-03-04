using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class AdamantiteYoyoItem : YoyoItem
    {
        public override string Texture => ModAssets.ItemsPath + "AdamantiteYoyo";
        public override int GamepadExtraRange => 15;

        public override void YoyoSetDefaults()
        {
            Item.damage = 5;
            Item.knockBack = 2.5f;

            Item.shoot = ModContent.ProjectileType<AdamantiteYoyoProjectile>();

            Item.rare = ItemRarityID.LightRed;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.AdamantiteBar, 12)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public class AdamantiteYoyoProjectile : YoyoProjectile
    {
        public override string Texture => ModAssets.ProjectilesPath + "AdamantiteYoyo";
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;
    }
}