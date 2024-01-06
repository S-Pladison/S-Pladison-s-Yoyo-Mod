using Terraria;

namespace SPYoyoMod.Utils.Extensions
{
    public static class PlayerExtensions
    {
        public static bool HasEquipped(this Player Player, int itemType)
        {
            for (int i = 3; i < 8 + Player.extraAccessorySlots; i++)
            {
                if (Player.armor[i].type.Equals(itemType)) return true;
            }

            return false;
        }
    }
}