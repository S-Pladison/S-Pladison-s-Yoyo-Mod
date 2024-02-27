using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Utils.DataStructures;
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
        /// <summary>
        /// More or less safe only for drawing.
        /// Don't use when update game logic.
        /// </summary>
        [Autoload(Side = ModSide.Client)]
        private class ActiveEntities : ILoadable
        {
            public static readonly List<Projectile> Projectiles;
            public static readonly List<NPC> NPCs;

            public static uint LastUpdateTick;

            static ActiveEntities()
            {
                Projectiles = new List<Projectile>();
                NPCs = new List<NPC>();

                LastUpdateTick = 0;
            }

            private static void UpdateActiveEntityLists()
            {
                if (LastUpdateTick.Equals(Main.GameUpdateCount)) return;

                Projectiles.Clear();
                NPCs.Clear();

                foreach (var proj in Main.projectile)
                {
                    if (!proj.active) continue;

                    Projectiles.Add(proj);
                }

                foreach (var npc in Main.npc)
                {
                    if (!npc.active) continue;

                    NPCs.Add(npc);
                }

                LastUpdateTick = Main.GameUpdateCount;
            }

            void ILoadable.Load(Mod mod)
            {
                ModEvents.OnPostUpdateEverything += UpdateActiveEntityLists;
            }

            void ILoadable.Unload()
            {
                Projectiles.Clear();
                NPCs.Clear();
            }
        }

        /// <summary>
        /// Returns all active entities.
        /// More or less safe only for drawing.
        /// Don't use when update game logic.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static IReadOnlyList<T> GetActiveForDrawEntities<T>() where T : Entity
        {
            if (typeof(T).Equals(typeof(Projectile)))
                return ActiveEntities.Projectiles as IReadOnlyList<T>;

            if (typeof(T).Equals(typeof(NPC)))
                return ActiveEntities.NPCs as IReadOnlyList<T>;

            throw new NotImplementedException();
        }

        /// <summary>
        /// Draw npc. And nothing more.
        /// </summary>
        public static void DrawNPC(NPC npc, bool behindTiles)
        {
            Main.instance.DrawNPC(npc.whoAmI, behindTiles);
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

                color = ColorUtils.MultipleLerp(colorEasing.Invoke(segmentIndex / (float)segmentCount), colors.Select(x => x.glow ? x.color : Lighting.GetColor(position.ToTileCoordinates(), x.color)).ToArray());
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

        public delegate void DrawYoyoStringSegmentDelegate(int segmentCount, int segmentIndex, Vector2 position, float rotation, float height, Color color);

        private static readonly TryApplyingPlayerStringColorDelegate tryApplyingPlayerStringColorFunc = typeof(Main)
            ?.GetMethod("TryApplyingPlayerStringColor", BindingFlags.NonPublic | BindingFlags.Static)
            ?.CreateDelegate<TryApplyingPlayerStringColorDelegate>()
            ?? throw new InvalidOperationException("Unable to acquire TryApplyingPlayerStringColor method delegate...");

        private delegate Color TryApplyingPlayerStringColorDelegate(int playerStringColor, Color defaultColor);
    }
}