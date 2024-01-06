using Terraria;

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
    }
}