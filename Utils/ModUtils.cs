﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace SPYoyoMod.Utils
{
    public static class ModUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SecondsToTicks(int seconds)
            => seconds * 60;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SecondsToTicks(float seconds)
            => (int)(seconds * 60);

        public static Asset<Effect> Prepare(this Asset<Effect> effect, Action<EffectParameterCollection> action)
        {
            if (!effect.IsLoaded)
                return effect;

            action(effect.Value.Parameters);

            return effect;
        }

        public static void Apply(this Asset<Effect> effect, string passName = null)
        {
            if (!effect.IsLoaded)
                return;

            if (passName == string.Empty)
                passName = effect.Value.CurrentTechnique.Passes.First().Name;

            effect.Value.CurrentTechnique.Passes[passName].Apply();
        }

        public static Func<T, V> GetFieldAccessor<T, V>(string fieldName)
        {
            var param = Expression.Parameter(typeof(T), "arg");
            var member = Expression.Field(param, fieldName);
            var lambda = Expression.Lambda(typeof(Func<T, V>), member, param);

            return lambda.Compile() as Func<T, V>;
        }

        public static Action<T, V> SetFieldAccessor<T, V>(string fieldName)
        {
            var param = Expression.Parameter(typeof(T), "arg");
            var valueParam = Expression.Parameter(typeof(V), "value");
            var member = Expression.Field(param, fieldName);
            var assign = Expression.Assign(member, valueParam);
            var lambda = Expression.Lambda(typeof(Action<T, V>), assign, param, valueParam);

            return lambda.Compile() as Action<T, V>;
        }

        public static void EmptyAction() { }
        public static void EmptyAction<T>(T _) { }
        public static void EmptyAction<T1, T2>(T1 _1, T2 _2) { }
    }
}