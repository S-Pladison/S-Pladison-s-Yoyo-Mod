using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SPYoyoMod.Utils
{
    /// <summary>
    /// Представляет кривую Безье, определенную набором точек управления.
    /// Кривая Безье — это математическая кривая, широко используемая в графике и анимации
    /// для плавного интерполирования между точками.
    /// 
    /// Основные возможности:
    /// <br/>- Вычисление точки на кривой для заданного параметра `t` (от 0 до 1),
    /// где `t = 0` соответствует начальной точке, а `t = 1` — конечной.
    /// <br/>- Генерация набора точек, равномерно распределенных вдоль кривой.
    /// <br/>
    /// <br/>Методы:
    /// <br/>- <see cref="Evaluate"/> — вычисляет координаты точки на кривой для заданного параметра `t`.
    /// <br/>- <see cref="GetPoints"/> — создает список из заданного количества точек,
    /// равномерно расположенных вдоль кривой.
    /// </summary>
    public sealed class BezierCurve(params Vector2[] points)
    {
        private static readonly IList<float> _factorialList = [1f];

        private readonly IList<Vector2> _points = points;

        /// <summary>
        /// Вычисляет координаты точки на кривой Безье для заданного значения параметра `t` (от 0 до 1),
        /// используя внутренние точки управления текущего экземпляра.
        /// </summary>
        /// <param name="t">Параметр на кривой (от 0 до 1), где 0 соответствует начальной точке, а 1 — конечной.</param>
        /// <returns>Координаты точки на кривой.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 Evaluate(float t)
            => EvaluateInternal(t, _points.AsReadOnly());

        /// <summary>
        /// Генерирует список точек, равномерно распределенных вдоль кривой Безье,
        /// используя внутренние точки управления текущего экземпляра.
        /// </summary>
        /// <param name="amount">Количество точек для генерации.</param>
        /// <returns>Список координат точек на кривой.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<Vector2> GetPoints(int amount)
            => GetPointsInternal(amount, _points.AsReadOnly());

        /// <summary>
        /// Вычисляет координаты точки на кривой Безье для заданного значения параметра `t` (от 0 до 1),
        /// используя указанный список точек управления.
        /// </summary>
        /// <param name="t">Параметр на кривой (от 0 до 1), где 0 соответствует начальной точке, а 1 — конечной.</param>
        /// <param name="points">Список точек управления, определяющий форму кривой.</param>
        /// <returns>Координаты точки на кривой.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Evaluate(float t, IList<Vector2> points)
            => EvaluateInternal(t, points.AsReadOnly());

        /// <summary>
        /// Вычисляет координаты точки на кривой Безье для заданного значения параметра `t` (от 0 до 1),
        /// используя указанный набор точек управления.
        /// </summary>
        /// <param name="t">Параметр на кривой (от 0 до 1), где 0 соответствует начальной точке, а 1 — конечной.</param>
        /// <param name="points">Массив точек управления, определяющий форму кривой.</param>
        /// <returns>Координаты точки на кривой.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Evaluate(float t, params Vector2[] points)
            => EvaluateInternal(t, points);

        /// <summary>
        /// Генерирует список точек, равномерно распределенных вдоль кривой Безье,
        /// используя указанный список точек управления.
        /// </summary>
        /// <param name="amount">Количество точек для генерации.</param>
        /// <param name="points">Список точек управления, определяющий форму кривой.</param>
        /// <returns>Список координат точек на кривой.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<Vector2> GetPoints(int amount, IList<Vector2> points)
            => GetPointsInternal(amount, points.AsReadOnly());

        /// <summary>
        /// Генерирует список точек, равномерно распределенных вдоль кривой Безье,
        /// используя указанный набор точек управления.
        /// </summary>
        /// <param name="amount">Количество точек для генерации.</param>
        /// <param name="points">Массив точек управления, определяющий форму кривой.</param>
        /// <returns>Список координат точек на кривой.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<Vector2> GetPoints(int amount, params Vector2[] points)
            => GetPointsInternal(amount, points);

        private static Vector2 EvaluateInternal(float t, IReadOnlyList<Vector2> points)
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

        private static List<Vector2> GetPointsInternal(int amount, IReadOnlyList<Vector2> points)
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
