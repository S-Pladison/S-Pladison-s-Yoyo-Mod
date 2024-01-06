using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    [Autoload(false)]
    public class BlackholeYoyoItem : YoyoItem
    {
        public BlackholeYoyoItem() : base(gamepadExtraRange: 15) { }

        public override void YoyoSetDefaults()
        {
            Item.damage = 43;
            Item.knockBack = 2.5f;

            Item.shoot = ModContent.ProjectileType<BlackholeYoyoProjectile>();

            Item.rare = ItemRarityID.Lime;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    [Autoload(false)]
    public class BlackholeYoyoProjectile : YoyoProjectile
    {
        public BlackholeYoyoProjectile() : base(lifeTime: -1f, maxRange: 300f, topSpeed: 13f) { }
    }
}