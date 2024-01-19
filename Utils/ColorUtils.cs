using Microsoft.Xna.Framework;
using System;

namespace SPYoyoMod.Utils
{
    public static class ColorUtils
    {
        public static Color Multiply(Color first, Color second)
            => new((byte)(first.R * second.R / 255f), (byte)(first.G * second.G / 255f), (byte)(first.B * second.B / 255f), (byte)(first.A * second.A / 255f));

        public static Color MultipleLerp(float t, params Color[] colors)
        {
            if (t >= 1) return colors[^1];

            t = Math.Max(t, 0);
            float num = 1f / (colors.Length - 1);
            int index = Math.Max(0, (int)(t / num));

            return Color.Lerp(colors[index], colors[index + 1], (t - num * index) / num);
        }
    }
}