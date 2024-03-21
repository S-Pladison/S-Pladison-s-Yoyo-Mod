using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    public static partial class EntityExtensions
    {
        public static bool HasEquipped(this Player player, int itemType)
        {
            for (var i = 3; i < 8 + player.extraAccessorySlots; i++)
            {
                if (player.armor[i].type.Equals(itemType)) return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OwnedProjectileCounts(this Player player, int projectileType)
        {
            return player.ownedProjectileCounts[projectileType];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OwnedProjectileCounts<T>(this Player player) where T : ModProjectile
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<T>()];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ProvideRandomCounterweight(this Player player)
        {
            player.counterWeight = 556 + Main.rand.Next(6);
        }
    }
}