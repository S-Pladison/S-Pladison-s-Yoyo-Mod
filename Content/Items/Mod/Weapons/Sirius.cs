using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class SiriusItem : YoyoItem
    {
        public override string Texture { get => ModAssets.ItemsPath + "Sirius"; }

        public SiriusItem() : base(gamepadExtraRange: 15) { }

        public override void YoyoSetDefaults()
        {
            Item.damage = 43;
            Item.knockBack = 2.5f;

            Item.shoot = ModContent.ProjectileType<SiriusProjectile>();

            Item.rare = ItemRarityID.Lime;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    public class SiriusProjectile : YoyoProjectile
    {
        public override string Texture { get => ModAssets.ProjectilesPath + "Sirius"; }

        public SiriusProjectile() : base(lifeTime: -1f, maxRange: 300f, topSpeed: 13f) { }
    }
}