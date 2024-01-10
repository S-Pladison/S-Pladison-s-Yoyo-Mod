using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class MyocardialInfarctionItem : YoyoItem
    {
        public override string Texture { get => ModAssets.ItemsPath + "MyocardialInfarction"; }

        public MyocardialInfarctionItem() : base(gamepadExtraRange: 15) { }

        public override void YoyoSetDefaults()
        {
            Item.damage = 43;
            Item.knockBack = 2.5f;
            Item.autoReuse = true;

            Item.shoot = ModContent.ProjectileType<MyocardialInfarctionProjectile>();

            Item.rare = ItemRarityID.Lime;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    public class MyocardialInfarctionProjectile : YoyoProjectile
    {
        public override string Texture { get => ModAssets.ProjectilesPath + "MyocardialInfarction"; }

        public MyocardialInfarctionProjectile() : base(lifeTime: -1f, maxRange: 300f, topSpeed: 13f) { }
    }
}