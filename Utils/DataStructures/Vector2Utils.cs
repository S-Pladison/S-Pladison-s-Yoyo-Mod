using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace SPYoyoMod.Utils
{
    public static partial class DataStructureExtensions
    {
        public static float DistanceBetween(this IList<Vector2> vectors)
        {
            return DistanceBetween(vectors as IReadOnlyList<Vector2>);
        }

        public static float DistanceBetween(this IReadOnlyList<Vector2> vectors)
        {
            var count = vectors.Count();

            if (count <= 1) return 0;

            var result = 0f;

            for (var i = 1; i < count; i++)
            {
                result += Vector2.Distance(vectors.ElementAt(i), vectors.ElementAt(i - 1));
            }

            return result;
        }
    }
}