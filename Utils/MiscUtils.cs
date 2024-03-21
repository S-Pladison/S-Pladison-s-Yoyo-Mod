using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Linq.Expressions;
using Terraria;

namespace SPYoyoMod.Utils
{
    public static class MiscUtils
    {
        public static void SpawnDustCircle(Vector2 center, float radius, int count, int type, Action<Dust> onSpawn = null)
        {
            for (var i = 0; i < count; i++)
            {
                var position = center + new Vector2(radius, 0).RotatedBy(i / (float)count * MathHelper.TwoPi);
                var dust = Dust.NewDustPerfect(position, type);
                onSpawn?.Invoke(dust);
            }
        }

        public static void SpawnDustCircle(Vector2 center, float radius, int count, Func<int, int> type, Action<Dust, int, float> onSpawn = null)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = i / (float)count * MathHelper.TwoPi;
                var position = center + new Vector2(radius, 0).RotatedBy(angle);
                var dustType = type?.Invoke(i) ?? -1;

                if (dustType != -1)
                {
                    var dust = Dust.NewDustPerfect(position, dustType);
                    onSpawn?.Invoke(dust, i, angle);
                }
            }
        }

        public static Asset<Effect> Prepare(this Asset<Effect> effect, Action<EffectParameterCollection> action)
        {
            if (!effect.IsLoaded)
                throw new Exception($"{effect.Name} is not loaded...");

            action(effect.Value.Parameters);

            return effect;
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
    }
}
