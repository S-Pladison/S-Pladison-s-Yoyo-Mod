using Microsoft.Xna.Framework;

namespace SPYoyoMod.Utils
{
    public static class CollisionUtils
    {
        /// <summary>
        /// Проверка пересечения между прямоугольником и окружностью.
        /// </summary>
        public static bool CheckRectanglevCircle(Rectangle rectangle, Vector2 circleCenter, float circleRadius)
        {
            var vector = new Vector2(
                MathHelper.Clamp(circleCenter.X, rectangle.Left, rectangle.Right),
                MathHelper.Clamp(circleCenter.Y, rectangle.Top, rectangle.Bottom)
            );

            var direction = circleCenter - vector;
            var distanceSquared = direction.LengthSquared();

            return distanceSquared <= circleRadius * circleRadius;
        }
    }
}