using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    /// <summary>
    /// Общий <see cref="ModPlayer"/> для кулдаунов.
    /// Устанавливай таймер через <see cref="Set{T}"/> или <see cref="Set(object, int)"/>; время уменьшается каждый игровой тик.
    /// Ключом может быть тип-метка, строка или любой другой объект.
    /// </summary>
    public sealed class CooldownPlayer : ModPlayer
    {
        private readonly Dictionary<object, Cooldown> _timers = [];

        /// <summary>
        /// Оставшееся время таймера по ключу в тиках.
        /// </summary>
        public int this[object key]
        {
            get => Get(key);
            set => Set(key, value);
        }

        /// <summary>
        /// Возвращает оставшееся время таймера <typeparamref name="T"/> в тиках.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get<T>() => Get(typeof(T));

        /// <summary>
        /// Возвращает оставшееся время таймера по ключу в тиках.
        /// </summary>
        public int Get(object key) => TryGet(key, out var cooldown) ? cooldown.Remaining : 0;

        /// <summary>
        /// Устанавливает таймер для метки <typeparamref name="T"/> на указанное количество тиков.
        /// Значение меньше либо равное нулю снимает таймер.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<T>(int ticks) => Set(typeof(T), ticks);

        /// <summary>
        /// Устанавливает таймер по ключу на указанное количество тиков.
        /// Значение меньше либо равное нулю снимает таймер.
        /// </summary>
        public void Set(object key, int ticks)
        {
            if (key is null)
                return;

            if (ticks <= 0)
            {
                _timers.Remove(key);
                return;
            }

            if (!_timers.TryGetValue(key, out var cooldown))
                _timers[key] = cooldown = new Cooldown();

            cooldown.Set(ticks);
        }

        /// <summary>
        /// Активен ли таймер с меткой <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActive<T>() => IsActive(typeof(T));

        /// <summary>
        /// Активен ли таймер с указанным ключом.
        /// </summary>
        public bool IsActive(object key) => TryGet(key, out var cooldown) && cooldown.IsActive;

        public override void PostUpdate()
        {
            foreach (var cooldown in _timers.Values)
                cooldown.Update();
        }

        private bool TryGet(object key, out Cooldown cooldown)
        {
            if (key is not null)
                return _timers.TryGetValue(key, out cooldown);

            cooldown = null;
            return false;
        }

        private sealed class Cooldown
        {
            public int Remaining { get; private set; }
            public bool IsActive => Remaining > 0;

            public void Set(int ticks) => Remaining = ticks > 0 ? ticks : 0;

            public void Update()
            {
                if (Remaining > 0)
                    Remaining--;
            }
        }
    }
}
