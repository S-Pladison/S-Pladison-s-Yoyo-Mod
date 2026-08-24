using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public sealed class CooldownPlayer : ModPlayer
    {
        private readonly Dictionary<object, Cooldown> _timers = [];

        public int this[object key]
        {
            get => Get(key);
            set => Set(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get<T>() => Get(typeof(T));

        public int Get(object key) => TryGet(key, out var cooldown) ? cooldown.Remaining : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<T>(int ticks) => Set(typeof(T), ticks);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActive<T>() => IsActive(typeof(T));

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
