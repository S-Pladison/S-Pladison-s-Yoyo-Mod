using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class TheStellarThrowItem : YoyoItem
    {
        public const int LootChanceDenominator = 3;

        public override string Texture => ModAssets.ItemsPath + "TheStellarThrow";
        public override int GamepadExtraRange => 15;

        public override void YoyoSetDefaults()
        {
            Item.damage = 43;
            Item.knockBack = 2.5f;

            Item.shoot = ModContent.ProjectileType<TheStellarThrowProjectile>();

            Item.rare = ItemRarityID.Green;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    public class TheStellarThrowProjectile : YoyoProjectile
    {
        public override string Texture => ModAssets.ProjectilesPath + "TheStellarThrow";
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;
    }
}