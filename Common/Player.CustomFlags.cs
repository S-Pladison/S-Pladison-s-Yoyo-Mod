using SPYoyoMod.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    [LoadBefore, LoadAfter(typeof(ModEvents))]
    public sealed class CustomFlagPlayer : ModPlayer
    {
        private readonly HashSet<string> _flags = [];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ToKey(Type type) => type.FullName ?? type.Name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has<T>() => Has(typeof(T));

        public bool Has(Type type) => type is not null && Has(ToKey(type));

        public bool Has(string key) => !string.IsNullOrEmpty(key) && _flags.Contains(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<T>() => Set(typeof(T));

        public void Set(Type type)
        {
            if (type is not null)
                Set(ToKey(type));
        }

        public void Set(string key)
        {
            if (!string.IsNullOrEmpty(key))
                _flags.Add(key);
        }

        public override void ResetEffects()
        {
            _flags.Clear();
        }
    }
}
