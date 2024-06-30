using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;

namespace SPYoyoMod.Utils
{
    public static class ItemUtils
    {
        /// <summary>
        /// Является ли этот предмет оружием типа йо-йо.
        /// </summary>
        public static bool IsYoyo(this Item item)
        {
            if (ItemID.Sets.Yoyo[item.type]) return true;
            if (item.shoot <= ProjectileID.None) return false;

            var dict = ContentSamples.ProjectilesByType;

            if (dict.TryGetValue(item.shoot, out Projectile proj))
                return proj.IsYoyo();

            proj = new Projectile();
            proj.SetDefaults(item.shoot);

            return proj.IsYoyo();
        }

        /// <summary>
        /// Конвертирует предоставленную стоимость продажи в медные монеты. Это значение в пять раз превышает <see cref="Item.buyPrice"/>.<br/>
        /// Если присвоено <see cref="Item.value"/>, то предмет будет продан за указанную стоимость.
        /// </summary>
        /// <returns>Преобразованное значение.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SellPrice(int platinum = 0, int gold = 0, int silver = 0, int copper = 0)
            => Item.sellPrice(platinum, gold, silver, copper);
    }
}