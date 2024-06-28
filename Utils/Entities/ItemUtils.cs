using System.Runtime.CompilerServices;
using Terraria;

namespace SPYoyoMod.Utils
{
    public static class ItemUtils
    {
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