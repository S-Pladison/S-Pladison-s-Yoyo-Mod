using SPYoyoMod.Common;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils.Entities
{
    public static class PlayerExtensions
    {
        public static bool HasEquipped(this Player player, int itemType)
        {
            for (var i = 3; i < 8 + player.extraAccessorySlots; i++)
            {
                if (player.armor[i].type.Equals(itemType)) return true;
            }

            return false;
        }

        public static void SetEffectFlag<T>(this Player player, bool value = true) where T : ModItem
            => player.GetModPlayer<PlayerEffectFlags>().SetFlag<T>(value);

        public static bool GetEffectFlag<T>(this Player player) where T : ModItem
            => player.GetModPlayer<PlayerEffectFlags>().GetFlag<T>();

        public static int OwnedProjectileCounts(this Player player, int projectileType)
            => player.ownedProjectileCounts[projectileType];

        public static int OwnedProjectileCounts<T>(this Player player) where T : ModProjectile
            => player.ownedProjectileCounts[ModContent.ProjectileType<T>()];

        public static void ProvideRandomCounterweight(this Player player)
            => player.counterWeight = 556 + Main.rand.Next(6);
    }
}