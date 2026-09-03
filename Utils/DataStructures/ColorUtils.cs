using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Graphics.Shaders;

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
            var num = 1f / (colors.Length - 1);
            var index = Math.Max(0, (int)(t / num));

            return Color.Lerp(colors[index], colors[index + 1], (t - num * index) / num);
        }

        /// <summary>
        /// Определяет цвет красителя для снарежения. Если красителя нет, то возвращает белый цвет.
        /// </summary>
        public static Color GetDyeColor(int dye, Player player)
        {
            if (dye <= 0)
                return Color.White;

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_uColor")]
            static extern ref Vector3 GetShaderUColor(ArmorShaderData shader);

            var shader = GameShaders.Armor.GetSecondaryShader(dye, player);
            return shader is null ? Color.White : new Color(GetShaderUColor(shader));
        }
    }
}