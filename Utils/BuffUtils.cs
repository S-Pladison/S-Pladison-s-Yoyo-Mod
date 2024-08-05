using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Utils
{
    public static class BuffUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsModBuff(int type)
            => type >= BuffID.Count;

        public static F[] GetGlobalHook<F>(Expression<Func<GlobalBuff, F>> expr) where F : Delegate
        {
            var query = expr.ToOverrideQuery();
            return (from t in _globalBuffs.Where(query.HasOverride) select (F)query.Binder(t)).ToArray();
        }

        private static readonly IList<GlobalBuff> _globalBuffs
            = typeof(BuffLoader).GetField("globalBuffs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as IList<GlobalBuff>;
    }
}
