using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace SPYoyoMod.Utils.DataStructures
{
    public static class Vector2Utils
    {
        /// <summary>
        /// Вычисление расстояния между упорядоченными точками.
        /// </summary>
        public static float Distance(this IReadOnlyList<Vector2> list)
        {
            var count = list.Count;

            if (count <= 1)
                return 0;

            var result = 0f;

            for (var i = 1; i < count; i++)
                result += Vector2.Distance(list[i], list[i - 1]);

            return result;
        }
    }
}