using System;
using System.Linq.Expressions;
using System.Reflection;

namespace SPYoyoMod.Utils
{
    public static class TypeUtils
    {
        public static Func<T, V> GetFieldAccessor<T, V>(string fieldName)
        {
            var param = Expression.Parameter(typeof(T), "arg");
            var member = Expression.Field(param, fieldName);
            var lambda = Expression.Lambda<Func<T, V>>(member, param);

            return lambda.Compile();
        }

        public static Action<T, V> SetFieldAccessor<T, V>(string fieldName)
        {
            var param = Expression.Parameter(typeof(T), "arg");
            var valueParam = Expression.Parameter(typeof(V), "value");
            var member = Expression.Field(param, fieldName);
            var assign = Expression.Assign(member, valueParam);
            var lambda = Expression.Lambda<Action<T, V>>(assign, param, valueParam);

            return lambda.Compile();
        }

        public static Func<P1, P2, P3, T> ConstructorAccessor<T, P1, P2, P3>()
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            var ctor = typeof(T).GetConstructor(flags, [typeof(P1), typeof(P2), typeof(P3)]);

            var param1 = Expression.Parameter(typeof(P1), "param1");
            var param2 = Expression.Parameter(typeof(P2), "param2");
            var param3 = Expression.Parameter(typeof(P3), "param3");

            var body = Expression.New(ctor, param1, param2, param3);
            var lambda = Expression.Lambda<Func<P1, P2, P3, T>>(body, param1, param2, param3);

            return lambda.Compile();
        }
    }
}