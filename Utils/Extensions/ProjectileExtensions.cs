using Terraria;

namespace SPYoyoMod.Utils.Extensions
{
    public static class ProjectileExtensions
    {
        public static bool IsYoyo(this Projectile projectile) { return projectile.aiStyle.Equals(99); }
    }
}