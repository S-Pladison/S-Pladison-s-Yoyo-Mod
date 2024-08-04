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
        /// Является ли этот снаряд основным снарядом от йо-йо.
        /// Основным является тот, которым управляет игрок, а не тот, который летает возле.
        /// Учитывайте, что основной йо-йо не обязательно будет тем, что заспавнился первым.
        /// </summary>
        public static bool IsMainYoyo(this Projectile proj)
        {
            if (!proj.IsYoyo() || proj.IsCounterweight())
                return false;

            for (int i = 0; i < proj.whoAmI; i++)
            {
                ref var otherProj = ref Main.projectile[i];

                if (otherProj.type == proj.type && otherProj.owner == proj.owner)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Является ли этот снаряд противовесом.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCounterweight(this Projectile proj)
            => proj.counterweight;

        /// <summary>
        /// Является ли этот снаряд снарядом ванильным.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVanilla(this Projectile proj)
            => proj.type < ProjectileID.Count;

        /// <summary>
        /// Является ли локальный игрок владельцем данного снаряда.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLocalPlayerAsOwner(this Projectile proj)
            => proj.owner == Main.myPlayer;

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