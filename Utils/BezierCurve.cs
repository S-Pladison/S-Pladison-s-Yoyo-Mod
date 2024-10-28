using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SPYoyoMod.Utils
{
    public sealed class BezierCurve
    {
        private static readonly IList<float> _factorialList = [1f];

        private readonly IList<Vector2> _points;

        public BezierCurve(params Vector2[] points)
        {
            _points = points;
        }

        public BezierCurve(IList<Vector2> points)
        {
            _points = points;
        }

        public Vector2 GetPoint(float t)
            => GetPoint(t, _points);
        public List<Vector2> GetPoints(int amount)
            => GetPoints(amount, _points);

        public static Vector2 GetPoint(float t, params Vector2[] points)
            => GetPoint(t, points);

        public static Vector2 GetPoint(float t, IList<Vector2> points)
        {
            var count = points.Count - 1;

            if (t <= 0) return points[0];
            if (t >= 1) return points[count];

            var point = Vector2.Zero;

            for (var i = 0; i < points.Count; i++)
            {
                point += Bernstein(count, i, t) * points[i];
            }

            return point;
        }

        public static List<Vector2> GetPoints(int amount, params Vector2[] points)
            => GetPoints(amount, points.ToList());

        public static List<Vector2> GetPoints(int amount, IList<Vector2> points)
        {
            var count = points.Count - 1;
            var result = new List<Vector2>();
            var perStep = 1f / (amount - 1);

            for (var i = 0; i < amount; i++)
            {
                var point = Vector2.Zero;

                for (var j = 0; j < points.Count; j++)
                {
                    point += Bernstein(count, j, perStep * i) * points[j];
                }

                result.Add(point);
            }

            return result;
        }

        private static float Bernstein(int n, int i, float t)
            => Factorial(n) / (Factorial(i) * Factorial(n - i)) * MathF.Pow(t, i) * MathF.Pow(1 - t, n - i);

        private static float Factorial(int n)
        {
            n = Math.Max(n, 0);

            if (n < _factorialList.Count) return _factorialList[n];

            for (var i = _factorialList.Count; i <= n; i++)
            {
                _factorialList.Add(_factorialList.Last() * i);
            }

            return _factorialList.Last();
        }
    }
}
