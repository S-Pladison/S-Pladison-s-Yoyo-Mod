using SPYoyoMod.Common.Global.Projectiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace SPYoyoMod.Utils.Extensions
{
    public static class ProjectileExtensions
    {
        public static bool IsYoyo(this Projectile projectile) { return projectile.aiStyle.Equals(YoyoGlobalProjectile.YoyoAIStyle); }
    }
}