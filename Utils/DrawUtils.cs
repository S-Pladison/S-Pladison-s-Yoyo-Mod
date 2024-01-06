using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;
using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;

namespace SPYoyoMod.Utils
{
    public static class DrawUtils
    {
        // Copy from Main.DrawProj_DrawYoyoString
        public static void DrawYoyoString(Projectile proj, Vector2 mountedCenter, DrawYoyoStringSegmentDelegate drawSegment)
        {
            // Написать это так, чтобы было понятно что я бл рисую

            var owner = Main.player[proj.owner];
            var vector = mountedCenter;
            var num2 = proj.Center.X - vector.X;
            var num3 = proj.Center.Y - vector.Y + proj.gfxOffY;
            var flag = true;

            if (num2 == 0f && num3 == 0f)
            {
                flag = false;
            }
            else
            {
                var num6 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
                num6 = 12f / num6;
                num2 *= num6;
                num3 *= num6;
                vector.X -= num2 * 0.1f;
                vector.Y -= num3 * 0.1f;
                num2 = proj.position.X + proj.width * 0.5f - vector.X;
                num3 = proj.position.Y + proj.height * 0.5f - vector.Y;
            }

            int counter = 0;

            while (flag)
            {
                var segmentHeight = 12f;
                var num8 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
                var num9 = num8;

                if (float.IsNaN(num8) || float.IsNaN(num9))
                {
                    flag = false;
                    continue;
                }

                if (num8 < 20f)
                {
                    segmentHeight = num8 - 8f;
                    flag = false;
                }

                num8 = 12f / num8;
                num2 *= num8;
                num3 *= num8;
                vector.X += num2;
                vector.Y += num3;
                num2 = proj.position.X + proj.width * 0.5f - vector.X;
                num3 = proj.position.Y + proj.height * 0.1f - vector.Y;

                if (num9 > 12f)
                {
                    var num10 = 0.3f;
                    var num11 = Math.Abs(proj.velocity.X) + Math.Abs(proj.velocity.Y);

                    if (num11 > 16f) num11 = 16f;

                    num11 = 1f - num11 / 16f;
                    num10 *= num11;
                    num11 = num9 / 80f;

                    if (num11 > 1f) num11 = 1f;

                    num10 *= num11;

                    if (num10 < 0f) num10 = 0f;

                    num10 *= num11;
                    num10 *= 0.5f;

                    if (num3 > 0f)
                    {
                        num3 *= 1f + num10;
                        num2 *= 1f - num10;
                    }
                    else
                    {
                        num11 = Math.Abs(proj.velocity.X) / 3f;

                        if (num11 > 1f) num11 = 1f;

                        num11 -= 0.5f;
                        num10 *= num11;

                        if (num10 > 0f) num10 *= 2f;

                        num3 *= 1f + num10;
                        num2 *= 1f - num10;
                    }
                }

                var segmentPosition = vector + TextureAssets.FishingLine.Size() * 0.5f - new Vector2(6f, 0f);
                var segmentRotation = (float)Math.Atan2(num3, num2) - 1.57f;
                var segmentColor = Color.White;
                segmentColor.A = (byte)(segmentColor.A * 0.4f);
                segmentColor = TryApplyingPlayerStringColor(owner.stringColor, segmentColor);
                segmentColor = Lighting.GetColor((int)vector.X / 16, (int)(vector.Y / 16f), segmentColor);

                drawSegment(counter++, segmentPosition, segmentRotation, segmentHeight, segmentColor);
            }
        }

        public delegate void DrawYoyoStringSegmentDelegate(int index, Vector2 position, float rotation, float height, Color color);

        private static Color TryApplyingPlayerStringColor(int playerStringColor, Color stringColor)
        {
            return (Color)(StringColorMethodInfo?.Invoke(null, new object[] { playerStringColor, stringColor }) ?? stringColor);
        }

        private static readonly MethodInfo StringColorMethodInfo = typeof(Main).GetMethod("TryApplyingPlayerStringColor", BindingFlags.NonPublic | BindingFlags.Static);
    }
}
