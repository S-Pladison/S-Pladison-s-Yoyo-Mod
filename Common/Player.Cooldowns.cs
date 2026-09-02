using SPYoyoMod.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    [LoadBefore, LoadAfter(typeof(ModEvents))]
    public sealed class CooldownPlayer : ModPlayer
    {
        private readonly Dictionary<string, Cooldown> _timers = [];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ToKey(Type type) => type.FullName ?? type.Name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get<T>() => Get(typeof(T));

        public int Get(Type type) => type is null ? 0 : Get(ToKey(type));

        public int Get(string key) => TryGet(key, out var cooldown) ? cooldown.Remaining : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<T>(int ticks) => Set(typeof(T), ticks);

        public void Set(Type type, int ticks)
        {
            if (type is not null)
                Set(ToKey(type), ticks);
        }

        public void Set(string key, int ticks)
        {
            if (string.IsNullOrEmpty(key) || ticks <= 0)
                return;

            if (!_timers.TryGetValue(key, out var cooldown))
                _timers[key] = cooldown = new Cooldown();

            cooldown.Set(ticks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActive<T>() => IsActive(typeof(T));

        public bool IsActive(Type type) => type is not null && IsActive(ToKey(type));

        public bool IsActive(string key) => TryGet(key, out var cooldown) && cooldown.IsActive;

        public override void PostUpdate()
        {
            foreach (var (key, cooldown) in _timers)
            {
                cooldown.Update();

                if (!cooldown.IsActive)
                    _timers.Remove(key);
            }
        }

        private bool TryGet(string key, out Cooldown cooldown)
        {
            if (!string.IsNullOrEmpty(key))
                return _timers.TryGetValue(key, out cooldown);

            cooldown = null;
            return false;
        }

        private sealed class Cooldown
        {
            public int Remaining { get; private set; }
            public bool IsActive => Remaining > 0;

            public void Set(int ticks)
            {
                if (ticks > Remaining)
                    Remaining = ticks;
            }

            public void Update()
            {
                if (Remaining > 0)
                    Remaining--;
            }
        }
    }
}
