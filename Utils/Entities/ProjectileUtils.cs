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
            => proj.aiStyle.Equals(ProjAIStyleID.Yoyo);

        /// <summary>
        /// Получить владельца (игрока) снаряда.
        /// </summary>
        public static Player? GetOwner(this Projectile proj)
        {
            if (!Main.player.IndexInRange(proj.owner))
                return null;
            
            var player = Main.player[proj.owner];

            if (player == null || !player.active)
                return null;

            return player;
        }
    }
}