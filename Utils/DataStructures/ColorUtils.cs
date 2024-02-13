using Microsoft.Xna.Framework;
using System;

namespace SPYoyoMod.Utils.DataStructures
{
    public static class ColorUtils
    {
        public static Color Multiply(Color first, Color second)
            => new((byte)(first.R * second.R / 255f), (byte)(first.G * second.G / 255f), (byte)(first.B * second.B / 255f), (byte)(first.A * second.A / 255f));

        public static Color MultipleLerp(float t, params Color[] colors)
        {
            if (t >= 1) return colors[^1];

            t = Math.Max(t, 0);
            var num = 1f / (colors.Length - 1);
            var index = Math.Max(0, (int)(t / num));

            return Color.Lerp(colors[index], colors[index + 1], (t - num * index) / num);
        }
    }

    public static class ColorExtensions
    {
        public static Color Multiply(this Color @this, Color other)
            => ColorUtils.Multiply(@this, other);

        public static Color MultiplyR(this Color color, float value)
            => color with { R = (byte)(color.R * value) };

        public static Color MultiplyG(this Color color, float value)
            => color with { G = (byte)(color.G * value) };

        public static Color MultiplyB(this Color color, float value)
            => color with { B = (byte)(color.B * value) };

        public static Color MultiplyA(this Color color, float value)
            => color with { A = (byte)(color.A * value) };
    }
}