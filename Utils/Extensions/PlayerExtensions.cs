using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils.Extensions
{
    public static class PlayerExtensions
    {
        public static bool HasEquipped(this Player player, int itemType)
        {
            for (int i = 3; i < 8 + player.extraAccessorySlots; i++)
            {
                if (player.armor[i].type.Equals(itemType)) return true;
            }

            return false;
        }

        public static int OwnedProjectileCounts(this Player player, int projectileType)
            => player.ownedProjectileCounts[projectileType];

        public static int OwnedProjectileCounts<T>(this Player player) where T : ModProjectile
            => player.ownedProjectileCounts[ModContent.ProjectileType<T>()];

        public static void ProvideRandomCounterweight(this Player player)
            => player.counterWeight = 556 + Main.rand.Next(6);
    }
}