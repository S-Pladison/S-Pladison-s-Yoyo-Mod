using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
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

            public static int[] ProjectileCount;
            public static uint LastUpdateTick;

            static ActiveEntities()
            {
                Projectiles = new List<Projectile>();
                NPCs = new List<NPC>();
                ProjectileCount = new int[ProjectileID.Count];

                LastUpdateTick = 0;
            }

            private static void ResizeArrays()
            {
                Array.Resize(ref ProjectileCount, ProjectileLoader.ProjectileCount);
            }

            private static void UpdateActiveEntityLists()
            {
                if (LastUpdateTick.Equals(Main.GameUpdateCount)) return;

                Projectiles.Clear();
                NPCs.Clear();

                for (int i = 0; i < ProjectileCount.Length; i++)
                {
                    ProjectileCount[i] = 0;
                }

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

                foreach (var proj in Projectiles)
                {
                    ProjectileCount[proj.type] += 1;
                }

                LastUpdateTick = Main.GameUpdateCount;
            }

            void ILoadable.Load(Mod mod)
            {
                ModEvents.OnPostSetupContent += ResizeArrays;
                ModEvents.OnPostUpdateEverything += UpdateActiveEntityLists;
            }

            void ILoadable.Unload()
            {
                Projectiles.Clear();
                NPCs.Clear();

                ProjectileCount = null;
            }
        }

        /// <summary>
        /// Returns all active entities.
        /// More or less safe only for drawing.
        /// Don't use when update game logic.
        /// </summary>
        public static IReadOnlyList<T> GetActiveForDrawEntities<T>() where T : Entity
        {
            if (typeof(T).Equals(typeof(Projectile)))
                return ActiveEntities.Projectiles as IReadOnlyList<T>;

            if (typeof(T).Equals(typeof(NPC)))
                return ActiveEntities.NPCs as IReadOnlyList<T>;

            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the number of active entities of this type.
        /// More or less safe only for drawing.
        /// Don't use when update game logic.
        /// </summary>
        public static int GetActiveEntityCount<T>(int entityType)
        {
            if (typeof(T).Equals(typeof(Projectile)))
                return ActiveEntities.ProjectileCount[entityType];

            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks whether there are any active entity of this type.
        /// More or less safe only for drawing.
        /// Don't use when update game logic.
        /// </summary>
        public static bool AnyActiveEntity<T>(int entityType)
            => GetActiveEntityCount<T>(entityType) > 0;

        /// <summary>
        /// Draw NPCs. And nothing more.
        /// </summary>
        public static void DrawNPC(NPC npc, bool behindTiles)
        {
            Main.instance.DrawNPC(npc.whoAmI, behindTiles);
        }

        /// <summary>
        /// Copy from <see cref="Main"/>.DrawProj_DrawYoyoString(...).
        /// </summary>
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

            var segments = new List<Tuple<Vector2, float, float, Color>>();

            var color = Color.White;
            color.A = (byte)(color.A * 0.40000000596046448);

            var stringColor = GetPlayerStringColor(Main.player[proj.owner].stringColor, color);

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

                    x = proj.position.X + proj.width * 0.5f - startPos.X;
                    y = proj.position.Y + proj.height * 0.1f - startPos.Y;

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

                    var segmentColor = Lighting.GetColor((int)startPos.X / 16, (int)(startPos.Y / 16.0), stringColor);
                    segmentColor = new Color(segmentColor.R * 0.5f, segmentColor.G * 0.5f, segmentColor.B * 0.5f, segmentColor.A * 0.5f);

                    var segmentPosition = new Vector2((float)(startPos.X + TextureAssets.FishingLine.Width() * 0.5), (float)(startPos.Y + TextureAssets.FishingLine.Height() * 0.5)) - new Vector2(6f, 0.0f);
                    var segmentRotation = (float)Math.Atan2((double)y, (double)x) - 1.57f;

                    segments.Add(new(segmentPosition, segmentRotation, segmentHeight, segmentColor));
                }
            }

            for (int i = 0; i < segments.Count; i++)
            {
                drawSegment(segments.Count, i, segments[i].Item1, segments[i].Item2, segments[i].Item3, segments[i].Item4);
            }
        }

        private static Color GetPlayerStringColor(int playerStringColor, Color defaultColor)
            => (Color)StringColorMethodInfo.Invoke(null, new object[] { playerStringColor, defaultColor });

        private static readonly MethodInfo StringColorMethodInfo
            = typeof(Main).GetMethod("TryApplyingPlayerStringColor", BindingFlags.NonPublic | BindingFlags.Static);

        public delegate void DrawYoyoStringSegmentDelegate(int segmentCount, int segmentIndex, Vector2 position, float rotation, float height, Color color);
    }
}