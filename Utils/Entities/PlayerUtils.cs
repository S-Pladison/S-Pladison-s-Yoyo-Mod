using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils.Entities
{
    public static class PlayerUtils
    {
        /// <summary>
        /// Возвращает количество снарядов типа <typeparamref name="T"/>, которыми владеет игрок.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OwnedProjectileCounts<T>(this Player player) where T : ModProjectile
            => player.ownedProjectileCounts[ModContent.ProjectileType<T>()];
    }
}