using Microsoft.Xna.Framework;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;

namespace SPYoyoMod.Utils
{
    public static class DrawUtils
    {
        // Copy from Main.DrawProj_DrawYoyoString
        public static void DrawYoyoString(Projectile proj, Vector2 mountedCenter, DrawYoyoStringSegmentDelegate drawSegment)
        {
            Vector2 startPos = mountedCenter;
            startPos.Y += Main.player[proj.owner].gfxOffY;

            float x = proj.Center.X - startPos.X;
            float y = proj.Center.Y - startPos.Y;

            bool flag1 = true;
            bool flag2 = true;

            if ((double)x == 0.0 && (double)y == 0.0)
            {
                flag1 = false;
            }
            else
            {
                float num4 = 12f / (float)Math.Sqrt((double)x * (double)x + (double)y * (double)y);
                float num5 = x * num4;
                float num6 = y * num4;

                startPos.X -= num5 * 0.1f;
                startPos.Y -= num6 * 0.1f;
                x = proj.position.X + (float)proj.width * 0.5f - startPos.X;
                y = proj.position.Y + (float)proj.height * 0.5f - startPos.Y;
            }

            int counter = 0;

            while (flag1)
            {
                float segmentHeight = 12f;
                float f1 = (float)Math.Sqrt((double)x * (double)x + (double)y * (double)y);
                float f2 = f1;

                if (float.IsNaN(f1) || float.IsNaN(f2))
                {
                    flag1 = false;
                }
                else
                {
                    if ((double)f1 < 20.0)
                    {
                        segmentHeight = f1 - 8f;
                        flag1 = false;
                    }

                    float num8 = 12f / f1;
                    float num9 = x * num8;
                    float num10 = y * num8;

                    if (flag2)
                    {
                        flag2 = false;
                    }
                    else
                    {
                        startPos.X += num9;
                        startPos.Y += num10;
                    }

                    x = proj.position.X + (float)proj.width * 0.5f - startPos.X;
                    y = proj.position.Y + (float)proj.height * 0.1f - startPos.Y;

                    if ((double)f2 > 12.0)
                    {
                        float num11 = 0.3f;
                        float num12 = Math.Abs(proj.velocity.X) + Math.Abs(proj.velocity.Y);

                        if ((double)num12 > 16.0) num12 = 16f;

                        float num13 = (float)(1.0 - (double)num12 / 16.0);
                        float num14 = num11 * num13;
                        float num15 = f2 / 80f;

                        if ((double)num15 > 1.0) num15 = 1f;

                        float num16 = num14 * num15;

                        if ((double)num16 < 0.0) num16 = 0.0f;

                        float num17 = num16 * num15 * 0.5f;

                        if ((double)y > 0.0)
                        {
                            y *= 1f + num17;
                            x *= 1f - num17;
                        }
                        else
                        {
                            float num18 = Math.Abs(proj.velocity.X) / 3f;

                            if ((double)num18 > 1.0) num18 = 1f;

                            float num19 = num18 - 0.5f;
                            float num20 = num17 * num19;

                            if ((double)num20 > 0.0) num20 *= 2f;

                            y *= 1f + num20;
                            x *= 1f - num20;
                        }
                    }

                    float segmentRotation = (float)Math.Atan2((double)y, (double)x) - 1.57f;
                    Color color = Color.White;
                    color.A = (byte)(color.A * 0.40000000596046448);
                    color = TryApplyingPlayerStringColor(Main.player[proj.owner].stringColor, color);
                    float num21 = 0.5f;
                    color = Lighting.GetColor((int)startPos.X / 16, (int)(startPos.Y / 16.0), color);
                    Color segmentColor = new(color.R * num21, color.G * num21, color.B * num21, color.A * num21);
                    var segmentPos = new Vector2((float)(startPos.X + TextureAssets.FishingLine.Width() * 0.5), (float)(startPos.Y + TextureAssets.FishingLine.Height() * 0.5)) - new Vector2(6f, 0.0f);

                    drawSegment(counter++, segmentPos, segmentRotation, segmentHeight, segmentColor);
                }
            }

            /*// Написать это так, чтобы было понятно что я бл рисую

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
            }*/
        }

        public delegate void DrawYoyoStringSegmentDelegate(int index, Vector2 position, float rotation, float height, Color color);

        private static Color TryApplyingPlayerStringColor(int playerStringColor, Color stringColor)
        {
            return (Color)(StringColorMethodInfo?.Invoke(null, new object[] { playerStringColor, stringColor }) ?? stringColor);
        }

        private static readonly MethodInfo StringColorMethodInfo = typeof(Main).GetMethod("TryApplyingPlayerStringColor", BindingFlags.NonPublic | BindingFlags.Static);
    }
}
