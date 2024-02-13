using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    // Референс - какое-та там оружие из спирит мода, создающее глыбу льда (меч Rimehowl)
    // При определенном условии будет создавать схожие глыбы по обе стороны от йо-йо
    // Под глыбами будут подниматься блоки (как в старой версии мода у каменного йо-йо)
    // Можно если че в сундук ледяной пихнуть

    public class FreezingPointItem : YoyoItem
    {
        public override string Texture => ModAssets.ItemsPath + "FreezingPoint";
        public override int GamepadExtraRange => 15;

        public override void YoyoSetDefaults()
        {
            Item.damage = 20;
            Item.knockBack = 2.5f;

            Item.shoot = ModContent.ProjectileType<FreezingPointProjectile>();

            Item.rare = ItemRarityID.Green;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    public class FreezingPointProjectile : YoyoProjectile
    {
        public override string Texture => ModAssets.ProjectilesPath + "FreezingPoint";
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;
    }
}