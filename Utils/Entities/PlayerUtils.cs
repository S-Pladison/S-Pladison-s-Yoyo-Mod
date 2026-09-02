using Microsoft.Xna.Framework;
using SPYoyoMod.Common;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    public static class PlayerUtils
    {
        /// <summary>
        /// Возвращает количество снарядов типа <typeparamref name="T"/>, которыми владеет игрок.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OwnedProjectileCounts<T>(this Player player) where T : ModProjectile
            => player.ownedProjectileCounts[ModContent.ProjectileType<T>()];

        /// <summary>
        /// Смещение спрайта тела в анимации игрока.
        /// </summary>
        public static Vector2 GetBodyFrameOffset(this Player player)
        {
            var offset = player.bodyPosition;
            var frame = player.bodyFrame.Y / player.bodyFrame.Height;

            if ((uint)frame < Main.OffsetsPlayerHeadgear.Length)
            {
                var headgear = Main.OffsetsPlayerHeadgear[frame];
                headgear.Y -= 2f;
                offset += new Vector2(headgear.X * player.direction, headgear.Y * player.gravDir);
            }

            return offset;
        }

        /// <summary>
        /// Возвращает оставшееся время кулдауна для <typeparamref name="T"/> в тиках.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCooldownFor<T>(this Player player)
            => player.GetModPlayer<CooldownPlayer>().Get<T>();

        /// <summary>
        /// Возвращает оставшееся время кулдауна для указанного ключа в тиках.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCooldownFor(this Player player, object key)
            => player.GetModPlayer<CooldownPlayer>().Get(key);

        /// <summary>
        /// Устанавливает кулдаун для <typeparamref name="T"/> на указанное количество тиков.
        /// Значение меньше либо равное нулю снимает кулдаун.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCooldownFor<T>(this Player player, int ticks)
            => player.GetModPlayer<CooldownPlayer>().Set<T>(ticks);

        /// <summary>
        /// Устанавливает кулдаун для указанного ключа на указанное количество тиков.
        /// Значение меньше либо равное нулю снимает кулдаун.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCooldownFor(this Player player, object key, int ticks)
            => player.GetModPlayer<CooldownPlayer>().Set(key, ticks);

        /// <summary>
        /// Активен ли кулдаун для <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCooldownActiveFor<T>(this Player player)
            => player.GetModPlayer<CooldownPlayer>().IsActive<T>();

        /// <summary>
        /// Активен ли кулдаун для указанного ключа.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCooldownActiveFor(this Player player, object key)
            => player.GetModPlayer<CooldownPlayer>().IsActive(key);

        /// <summary>
        /// Получить информацию о предмете касательно того, установлен ли он в слотах снаряжения.
        /// Можно узнать, установлен ли предмет в функциональный слот, в слот визуала, и должен ли он использовать краситель.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EquipmentInfo GetEquipmentInfoFor<T>(this Player player) where T : ModItem
            => player.GetModPlayer<EquipmentPlayer>().Get<T>();

        /// <summary>
        /// Получить информацию о предмете касательно того, установлен ли он в слотах снаряжения.
        /// Можно узнать, установлен ли предмет в функциональный слот, в слот визуала, и должен ли он использовать краситель.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EquipmentInfo GetEquipmentInfoFor(this Player player, int itemType)
            => player.GetModPlayer<EquipmentPlayer>().Get(itemType);
    }
}
