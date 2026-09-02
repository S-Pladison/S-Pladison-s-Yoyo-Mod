using Microsoft.Xna.Framework;
using SPYoyoMod.Common;
using System;
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
        /// Возвращает оставшееся время кулдауна для указанного типа в тиках.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCooldown(this Player player, Type type)
            => player.GetModPlayer<CooldownPlayer>().Get(type);

        /// <summary>
        /// Возвращает оставшееся время кулдауна для указанного ключа в тиках.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCooldown(this Player player, string key)
            => player.GetModPlayer<CooldownPlayer>().Get(key);

        /// <summary>
        /// Устанавливает кулдаун для <typeparamref name="T"/> на указанное количество тиков.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCooldownFor<T>(this Player player, int ticks)
            => player.GetModPlayer<CooldownPlayer>().Set<T>(ticks);

        /// <summary>
        /// Устанавливает кулдаун для указанного типа на указанное количество тиков.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCooldown(this Player player, Type type, int ticks)
            => player.GetModPlayer<CooldownPlayer>().Set(type, ticks);

        /// <summary>
        /// Устанавливает кулдаун для указанного ключа на указанное количество тиков.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCooldown(this Player player, string key, int ticks)
            => player.GetModPlayer<CooldownPlayer>().Set(key, ticks);

        /// <summary>
        /// Активен ли кулдаун для <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCooldownActiveFor<T>(this Player player)
            => player.GetModPlayer<CooldownPlayer>().IsActive<T>();

        /// <summary>
        /// Активен ли кулдаун для указанного типа.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCooldownActive(this Player player, Type type)
            => player.GetModPlayer<CooldownPlayer>().IsActive(type);

        /// <summary>
        /// Активен ли кулдаун для указанного ключа.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCooldownActive(this Player player, string key)
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
        public static EquipmentInfo GetEquipmentInfo(this Player player, int itemType)
            => player.GetModPlayer<EquipmentPlayer>().Get(itemType);

        /// <summary>
        /// Устанавливает пользовательский флаг для <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCustomFlagFor<T>(this Player player)
            => player.GetModPlayer<CustomFlagPlayer>().Set<T>();

        /// <summary>
        /// Устанавливает пользовательский флаг для указанного типа.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCustomFlag(this Player player, Type type)
            => player.GetModPlayer<CustomFlagPlayer>().Set(type);

        /// <summary>
        /// Устанавливает пользовательский флаг с указанным ключем.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCustomFlag(this Player player, string key)
            => player.GetModPlayer<CustomFlagPlayer>().Set(key);

        /// <summary>
        /// Активен ли пользовательский флаг для <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasCustomFlagFor<T>(this Player player)
            => player.GetModPlayer<CustomFlagPlayer>().Has<T>();

        /// <summary>
        /// Активен ли пользовательский флаг для указанного типа.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasCustomFlag(this Player player, Type type)
            => player.GetModPlayer<CustomFlagPlayer>().Has(type);

        /// <summary>
        /// Активен ли пользовательский флаг с указанным ключем.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasCustomFlag(this Player player, string key)
            => player.GetModPlayer<CustomFlagPlayer>().Has(key);
    }
}
