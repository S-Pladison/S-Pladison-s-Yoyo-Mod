using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Graphics;
using SPYoyoMod.Common.Graphics.Renderers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils.Rendering
{
    public static class DrawUtils
    {
        [Autoload(Side = ModSide.Client)]
        private class StripRenderer : ILoadable
        {
            public const int MaxVertices = 2 * 200;
            public const int MaxIndices = 6 * (200 - 1);

            public PrimitiveRenderer Renderer;
            public Vertex2DPositionColorTexture[] Vertices;
            public short[] Indices;
            public int InitIndicesCount;

            void ILoadable.Load(Mod mod)
            {
                Main.QueueMainThreadAction(() =>
                {
                    Renderer = new PrimitiveRenderer(MaxVertices, MaxIndices);
                });

                Vertices = new Vertex2DPositionColorTexture[MaxVertices];
                Indices = new short[MaxIndices];

                for (int i = 0; i < Vertices.Length; i++)
                {
                    Vertices[i].Color = Color.White;
                }
            }

            void ILoadable.Unload()
            {
                Main.QueueMainThreadAction(() =>
                {
                    Renderer?.Dispose();
                });
            }
        }

        /// <summary>
        /// Draw primitive strip.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void DrawPrimitiveStrip(Effect effect, IList<Vector2> points, StripWidthDelegate width, bool loop = false)
        {
            DrawPrimitiveStrip(effect, points.ToArray(), width, loop);
        }

        /// <summary>
        /// Draw primitive strip.
        /// </summary>
        public static void DrawPrimitiveStrip(Effect effect, Vector2[] points, StripWidthDelegate width, bool loop = false)
        {
            if (points is null || points.Length < 2) return;

            var stripRenderer = ModContent.GetInstance<StripRenderer>();

            if (stripRenderer is null) return;

            var segmentCount = points.Length + (loop ? 0 : -1);

            // Indices

            var maxIndices = 6 * segmentCount;

            if (maxIndices > stripRenderer.InitIndicesCount)
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                void AddIndex(ref int index, int value)
                {
                    stripRenderer.Indices[index++] = (short)value;
                }

                for (var i = stripRenderer.InitIndicesCount / 6; i < maxIndices / 6; i++)
                {
                    var index = i * 6;
                    var i2 = i * 2;
                    var j2 = (i + 1) * 2;

                    AddIndex(ref index, i2);
                    AddIndex(ref index, i2 + 1);
                    AddIndex(ref index, j2 + 1);
                    AddIndex(ref index, j2 + 1);
                    AddIndex(ref index, j2);
                    AddIndex(ref index, i2);
                }

                stripRenderer.InitIndicesCount = maxIndices;
            }

            // Factors from start to end

            var accumulativeLength = 0f;
            var lengths = new float[segmentCount];
            var totalLength = 0f;
            var factorsFromStartToEnd = new float[segmentCount];

            for (var i = 0; i < points.Length - 1; i++)
            {
                lengths[i] = Vector2.DistanceSquared(points[i], points[i + 1]);
                totalLength += lengths[i];
            }

            if (loop)
            {
                lengths[^1] = Vector2.DistanceSquared(points[^1], points[0]);
                totalLength += lengths[^1];
            }

            for (var i = 0; i < segmentCount; i++)
            {
                accumulativeLength += lengths[i];
                factorsFromStartToEnd[i] = accumulativeLength / totalLength;
            }

            // Vertex positions

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AddVertexPosition(ref int vertexIndex, Vector2 position)
            {
                stripRenderer.Vertices[vertexIndex++].Position = position;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            Vector2 RotateClockwiseNinety(Vector2 vector)
            {
                return new(-vector.Y, vector.X);
            }

            var vertexIndex = 0;
            var normal = RotateClockwiseNinety((loop ? (points[0] - points[^1]) : (points[1] - points[0])).SafeNormalize(Vector2.Zero));
            var halfWidth = width(0f) / 2f;
            var offset = normal * halfWidth;

            AddVertexPosition(ref vertexIndex, points[0] + offset);
            AddVertexPosition(ref vertexIndex, points[0] - offset);

            for (var i = 1; i < points.Length; i++)
            {
                normal = RotateClockwiseNinety((points[i] - points[i - 1]).SafeNormalize(Vector2.Zero));
                halfWidth = width(factorsFromStartToEnd[i - 1]) / 2f;
                offset = normal * halfWidth;

                AddVertexPosition(ref vertexIndex, points[i] + offset);
                AddVertexPosition(ref vertexIndex, points[i] - offset);
            }

            if (loop)
            {
                normal = RotateClockwiseNinety((points[0] - points[^1]).SafeNormalize(Vector2.Zero));
                halfWidth = width(1f) / 2f;
                offset = normal * halfWidth;

                AddVertexPosition(ref vertexIndex, points[0] + offset);
                AddVertexPosition(ref vertexIndex, points[0] - offset);
            }

            // Vertex UVs

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AddVertexUV(ref int vertexIndex, Vector2 uv)
            {
                stripRenderer.Vertices[vertexIndex++].TextureCoordinate = uv;
            }

            vertexIndex = 0;

            AddVertexUV(ref vertexIndex, Vector2.Zero);
            AddVertexUV(ref vertexIndex, Vector2.UnitY);

            for (var i = 0; i < factorsFromStartToEnd.Length; i++)
            {
                AddVertexUV(ref vertexIndex, new Vector2(factorsFromStartToEnd[i], 0));
                AddVertexUV(ref vertexIndex, new Vector2(factorsFromStartToEnd[i], 1));
            }

            // Prepare

            stripRenderer.Renderer.SetVertices(stripRenderer.Vertices);
            stripRenderer.Renderer.SetIndices(stripRenderer.Indices);

            // Draw

            var vertexCount = 2 * (segmentCount + 1);
            var indexCount = 6 * segmentCount;

            stripRenderer.Renderer.Draw(effect, vertexCount, indexCount / 3);
        }

        /// <summary>
        /// Draw npc. And nothing more.
        /// </summary>
        public static void DrawNPC(NPC npc, bool? behindTiles = null)
        {
            var oldPosition = npc.position;
            npc.position += npc.netOffset;

            Main.instance.DrawNPC(npc.whoAmI, behindTiles ?? npc.behindTiles);

            npc.position = oldPosition;
        }

        /// <summary>
        /// Draw gradient yoyo string with shadow. Code copied from <see cref="Main"/>.DrawProj_DrawYoyoString(...).
        /// </summary>
        public static void DrawGradientYoyoStringWithShadow(Projectile proj, Vector2 mountedCenter, params (Color color, bool glow)[] colors)
        {
            var easing = new EasingFunctions.EasingDelegate(
                t => t < 0.22f ? 0f : EasingFunctions.InOutCubic((t - 0.22f) / 0.78f)
            );

            DrawGradientYoyoStringWithShadow(proj, mountedCenter, easing, colors);
        }

        /// <summary>
        /// Draw gradient yoyo string with shadow. Code copied from <see cref="Main"/>.DrawProj_DrawYoyoString(...).
        /// </summary>
        public static void DrawGradientYoyoStringWithShadow(Projectile proj, Vector2 mountedCenter, EasingFunctions.EasingDelegate colorEasing, params (Color color, bool glow)[] colors)
        {
            DrawYoyoString(proj, mountedCenter, (segmentCount, segmentIndex, position, rotation, height, color) =>
            {
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "FishingLine_WithShadow", AssetRequestMode.ImmediateLoad);
                var rect = new Rectangle(0, 0, texture.Width(), (int)height);
                var origin = new Vector2(texture.Width() * 0.5f, 0f);

                color = DataStructureUtils.MultipleLerp(colorEasing.Invoke(segmentIndex / (float)segmentCount), colors.Select(x => x.glow ? x.color : Lighting.GetColor(position.ToTileCoordinates(), x.color)).ToArray());
                position -= Main.screenPosition;

                Main.spriteBatch.Draw(texture.Value, position, rect, color, rotation, origin, 1f, SpriteEffects.None, 0f);
            });
        }

        /// <summary>
        /// Draw yoyo string. Code copied from <see cref="Main"/>.DrawProj_DrawYoyoString(...).
        /// </summary>
        public static void DrawYoyoString(Projectile proj, Vector2 mountedCenter, DrawYoyoStringSegmentDelegate drawSegment)
        {
            var startPos = mountedCenter;
            startPos.Y += Main.player[proj.owner].gfxOffY;

            var x = proj.Center.X - startPos.X;
            var y = proj.Center.Y - startPos.Y;

            var flag1 = true;
            var flag2 = true;

            if ((double)x == 0.0 && (double)y == 0.0)
            {
                flag1 = false;
            }
            else
            {
                var num4 = 12f / (float)Math.Sqrt((double)x * (double)x + (double)y * (double)y);
                var num5 = x * num4;
                var num6 = y * num4;

                startPos.X -= num5 * 0.1f;
                startPos.Y -= num6 * 0.1f;
                x = proj.position.X + proj.width * 0.5f - startPos.X;
                y = proj.position.Y + proj.height * 0.5f - startPos.Y;
            }

            var segments = new List<Tuple<Vector2, float, float, Color>>();
            var stringColor = tryApplyingPlayerStringColorFunc(Main.player[proj.owner].stringColor, Color.White with { A = (byte)(255 * 0.40000000596046448) });

            while (flag1)
            {
                var segmentHeight = 12f;

                var f1 = (float)Math.Sqrt((double)x * (double)x + (double)y * (double)y);
                var f2 = f1;

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

                    var num8 = 12f / f1;
                    var num9 = x * num8;
                    var num10 = y * num8;

                    if (flag2)
                    {
                        flag2 = false;
                    }
                    else
                    {
                        startPos.X += num9;
                        startPos.Y += num10;
                    }

                    x = proj.position.X + proj.width * 0.5f - startPos.X;
                    y = proj.position.Y + proj.height * 0.1f - startPos.Y;

                    if ((double)f2 > 12.0)
                    {
                        var num11 = 0.3f;
                        var num12 = Math.Abs(proj.velocity.X) + Math.Abs(proj.velocity.Y);

                        if ((double)num12 > 16.0) num12 = 16f;

                        var num13 = (float)(1.0 - (double)num12 / 16.0);
                        var num14 = num11 * num13;
                        var num15 = f2 / 80f;

                        if ((double)num15 > 1.0) num15 = 1f;

                        var num16 = num14 * num15;

                        if ((double)num16 < 0.0) num16 = 0.0f;

                        var num17 = num16 * num15 * 0.5f;

                        if ((double)y > 0.0)
                        {
                            y *= 1f + num17;
                            x *= 1f - num17;
                        }
                        else
                        {
                            var num18 = Math.Abs(proj.velocity.X) / 3f;

                            if ((double)num18 > 1.0) num18 = 1f;

                            var num19 = num18 - 0.5f;
                            var num20 = num17 * num19;

                            if ((double)num20 > 0.0) num20 *= 2f;

                            y *= 1f + num20;
                            x *= 1f - num20;
                        }
                    }

                    var segmentColor = Lighting.GetColor((int)startPos.X / 16, (int)(startPos.Y / 16.0), stringColor);
                    segmentColor = new Color(segmentColor.R * 0.5f, segmentColor.G * 0.5f, segmentColor.B * 0.5f, segmentColor.A * 0.5f);

                    var segmentPosition = new Vector2((float)(startPos.X + TextureAssets.FishingLine.Width() * 0.5), (float)(startPos.Y + TextureAssets.FishingLine.Height() * 0.5)) - new Vector2(6f, 0.0f);
                    var segmentRotation = (float)Math.Atan2((double)y, (double)x) - 1.57f;

                    segments.Add(new(segmentPosition, segmentRotation, segmentHeight, segmentColor));
                }
            }

            for (var i = 0; i < segments.Count; i++)
            {
                drawSegment(segments.Count, i, segments[i].Item1, segments[i].Item2, segments[i].Item3, segments[i].Item4);
            }
        }

        private static readonly TryApplyingPlayerStringColorDelegate tryApplyingPlayerStringColorFunc = typeof(Main)
            ?.GetMethod("TryApplyingPlayerStringColor", BindingFlags.NonPublic | BindingFlags.Static)
            ?.CreateDelegate<TryApplyingPlayerStringColorDelegate>()
            ?? throw new InvalidOperationException("Unable to acquire TryApplyingPlayerStringColor method delegate...");

        private delegate Color TryApplyingPlayerStringColorDelegate(int playerStringColor, Color defaultColor);
    }

    public delegate float StripWidthDelegate(float factorFromStartToEnd);
    public delegate void DrawYoyoStringSegmentDelegate(int segmentCount, int segmentIndex, Vector2 position, float rotation, float height, Color color);
}