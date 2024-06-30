using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;

namespace SPYoyoMod.Utils
{
    public static class ProjectileUtils
    {
        /// <summary>
        /// Является ли этот снаряд снарядом от йо-йо. Снаряды противовесов также относятся к йо-йо.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsYoyo(this Projectile proj)
        {
            return proj.aiStyle.Equals(ProjAIStyleID.Yoyo);
        }
    }
}