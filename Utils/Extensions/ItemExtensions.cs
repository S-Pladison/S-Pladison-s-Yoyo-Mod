using Terraria;
using Terraria.ID;

namespace SPYoyoMod.Utils.Extensions
{
    public static class ItemExtensions
    {
        public static bool IsYoyo(this Item item)
        {
            if (ItemID.Sets.Yoyo[item.type]) return true;
            if (item.shoot <= ProjectileID.None) return false;

            var proj = new Projectile();
            proj.SetDefaults(item.shoot);

            return proj.IsYoyo();
        }
    }
}