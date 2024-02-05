using Microsoft.Xna.Framework;

namespace SPYoyoMod.Utils.Extensions
{
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