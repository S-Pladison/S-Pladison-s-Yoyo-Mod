using Terraria;
using Terraria.ID;

namespace SPYoyoMod.Utils
{
    public static partial class EntityExtensions
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