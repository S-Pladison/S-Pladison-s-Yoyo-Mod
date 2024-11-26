using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Core.Graphics.RenderTargets;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using IPostHook = SPYoyoMod.Core.Hooks.IPostDrawPixelatedProjectile;
using IPreHook = SPYoyoMod.Core.Hooks.IPreDrawPixelatedProjectile;

namespace SPYoyoMod.Core.Hooks
{
    /// <inheritdoc cref="IDrawPixelatedProjectile" />
    public interface IPreDrawPixelatedProjectile
    {
        internal static readonly GlobalHookList<GlobalProjectile> _hook =
            ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(i => ((IPreHook)i).PreDrawPixelated));

        /// <summary>
        /// Позволяет отрисовывать пикселизированные эффекты позади снаряда.
        /// </summary>
        void PreDrawPixelated(Projectile proj);
    }

    /// <inheritdoc cref="IDrawPixelatedProjectile" />
    public interface IPostDrawPixelatedProjectile
    {
        internal static readonly GlobalHookList<GlobalProjectile> _hook =
            ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(i => ((IPostHook)i).PostDrawPixelated));

        /// <summary>
        /// Позволяет отрисовывать пикселизированные эффекты поверх снаряда.
        /// </summary>
        void PostDrawPixelated(Projectile proj);
    }

    /// <summary>
    /// Позволяет снаряду отрисовывать пикселизированные эффекты.
    /// <br/>Интерфейс относится к следующим классам: <see cref="ModProjectile"/> и <see cref="GlobalProjectile"/>
    /// </summary>
    public interface IDrawPixelatedProjectile : IPreHook, IPostHook
    {
        // Примечание: Данная реализация не учитывает отрисовку скрытых снарядов (proj.hide)
        // Ясное дело, добавить это не так сложно, но на данный момент это просто не нужно...
        [Autoload(Side = ModSide.Client)]
        private sealed class DrawPixelatedProjectileImplementation : ILoadable
        {
            private static ProjectilePixelatedLayer _preDrawProjectilesLayer;
            private static ProjectilePixelatedLayer _postDrawProjectilesLayer;
            private static ProjectilePixelatedLayer _postDrawPlayersAfterProjectilesLayer;

            public void Load(Mod mod)
            {
                _preDrawProjectilesLayer = new(PreDrawPixelatedProjectiles);
                _postDrawProjectilesLayer = new(PostDrawPixelatedProjectiles);
                _postDrawPlayersAfterProjectilesLayer = new(PostDrawPixelatedProjectiles);

                ModEvents.OnPostUpdateCameraPosition += RenderProjectilesToTargets;

                // Хук для отрисовки большинства снарядов (без proj.hide и тех, что не в руке игрока)
                On_Main.DrawProjectiles += (orig, main) =>
                {
                    _preDrawProjectilesLayer.DrawToScreen();
                    orig(main);
                    _postDrawProjectilesLayer.DrawToScreen();
                };

                // Хуки для отрисовка пиксельных снарядов у снарядов, что расположены в руках игрока
                {
                    // - Зачем нужны IL_Main, если можно сделать то же самое, но с On_Main?
                    // Не хочу делать постоянные проверки/поиски нужного списка...
                    // Пример:
                    // void DrawCachedProjs(List<int> projs) {
                    //   if (projs == Main.instance.DrawCacheProjsBehindNPCs) {}
                    //   else (projs == Main.instance.DrawCacheProjsBehindNPCsAndTiles) {}
                    //   else (...) {}
                    //   ...
                    // }
                    // А текущим методом: просто вызовем наши функции без каких либо проверок
                    // Такая проблема есть у таких методов, как DrawCachedNPCs и DrawCachedProjs (а может и еще есть, хз)

                    IL_Main.DoDraw += (il) =>
                    {
                        Impl_DrawPlayers_AfterProjectiles(new ILCursor(il));
                    };

                    IL_Main.DrawCapture += (il) =>
                    {
                        Impl_DrawPlayers_AfterProjectiles(new ILCursor(il));
                    };
                }
            }

            public void Unload()
            {
                ModEvents.OnPostUpdateCameraPosition -= RenderProjectilesToTargets;

                _postDrawPlayersAfterProjectilesLayer = null;
                _postDrawProjectilesLayer = null;
                _preDrawProjectilesLayer = null;
            }

            private static List<int> FindOnscreenProjectiles()
            {
                var onscreenProjs = new List<int>(Main.projectile.Length / 8);

                foreach (var proj in Main.ActiveProjectiles)
                {
                    var offscreenDistance = ProjectileID.Sets.DrawScreenCheckFluff[proj.type];
                    var visibleRectangle = new Rectangle((int)Main.Camera.ScaledPosition.X - offscreenDistance, (int)Main.Camera.ScaledPosition.Y - offscreenDistance, (int)Main.Camera.ScaledSize.X + offscreenDistance * 2, (int)Main.Camera.ScaledSize.Y + offscreenDistance * 2);

                    if (!visibleRectangle.Intersects(proj.Hitbox))
                        continue;

                    onscreenProjs.Add(proj.whoAmI);
                }

                return onscreenProjs;
            }

            private static void RenderProjectilesToTargets()
            {
                var onscreenProjs = FindOnscreenProjectiles();

                if (onscreenProjs.Count == 0)
                    return;

                var onscreenProjSet = onscreenProjs.ToHashSet();
                var playerHeldProjs = new List<int>(Main.player.Length / 4);
                var notHiddenProjs = new List<int>(Main.projectile.Length / 8);

                foreach (var player in Main.ActivePlayers)
                {
                    if (player.heldProj >= 0 && onscreenProjSet.Contains(player.heldProj))
                        playerHeldProjs.Add(player.heldProj);
                }

                foreach (var projIndex in onscreenProjs)
                {
                    ref var proj = ref Main.projectile[projIndex];

                    if (!proj.hide)
                        notHiddenProjs.Add(proj.whoAmI);
                }

                _preDrawProjectilesLayer.RenderToTarget(notHiddenProjs);
                _postDrawProjectilesLayer.RenderToTarget(notHiddenProjs.Except(playerHeldProjs).ToArray());
                _postDrawPlayersAfterProjectilesLayer.RenderToTarget(playerHeldProjs);
            }

            private static bool PreDrawPixelatedProjectiles(IReadOnlyList<int> projs)
            {
                var anyDrawCalls = false;

                foreach (var projIndex in projs)
                {
                    ref var proj = ref Main.projectile[projIndex];

                    try
                    {
                        if (proj.ModProjectile is IPreHook modProj)
                        {
                            modProj.PreDrawPixelated(proj);
                            anyDrawCalls = true;
                        }

                        foreach (IPreHook g in IPreHook._hook.Enumerate(proj))
                        {
                            g.PreDrawPixelated(proj);
                            anyDrawCalls = true;
                        }
                    }
                    catch (Exception)
                    {
                        proj.active = false;
                    }
                }

                return anyDrawCalls;
            }

            private static bool PostDrawPixelatedProjectiles(IReadOnlyList<int> projs)
            {
                var anyDrawCalls = false;

                foreach (var projIndex in projs)
                {
                    ref var proj = ref Main.projectile[projIndex];

                    try
                    {
                        if (proj.ModProjectile is IPostHook modProj)
                        {
                            modProj.PostDrawPixelated(proj);
                            anyDrawCalls = true;
                        }

                        foreach (IPostHook g in IPostHook._hook.Enumerate(proj))
                        {
                            g.PostDrawPixelated(proj);
                            anyDrawCalls = true;
                        }
                    }
                    catch (Exception)
                    {
                        proj.active = false;
                    }
                }

                return anyDrawCalls;
            }

            private static void Impl_DrawPlayers_AfterProjectiles(ILCursor cursor)
            {
                // DrawCachedProjs(DrawCacheProjsOverPlayers);

                // IL_1762: ldarg.0
                // IL_1763: ldarg.0
                // IL_1764: ldfld class [System.Collections] System.Collections.Generic.List`1<int32> Terraria.Main::DrawCacheProjsOverPlayers
                // IL_1769: ldc.i4.1
                // IL_176a: call instance void Terraria.Main::DrawCachedProjs(class [System.Collections] System.Collections.Generic.List`1<int32>, bool)

                if (!cursor.TryGotoNext(
                    MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Main>("DrawCacheProjsOverPlayers"),
                    i => i.MatchLdcI4(1),
                    i => i.MatchCall(typeof(Main).GetMethod("DrawCachedProjs", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(List<int>), typeof(bool)]))))
                {
                    ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(Impl_DrawPlayers_AfterProjectiles)}\" failed...");
                    return;
                }

                cursor.Emit(OpCodes.Ldsfld, typeof(DrawPixelatedProjectileImplementation).GetField(nameof(_postDrawPlayersAfterProjectilesLayer), BindingFlags.Static | BindingFlags.NonPublic));
                cursor.Emit(OpCodes.Call, typeof(ProjectilePixelatedLayer).GetMethod(nameof(ProjectilePixelatedLayer.DrawToScreen), BindingFlags.Instance | BindingFlags.Public));
            }
        }

        private sealed class ProjectilePixelatedLayer(Func<IReadOnlyList<int>, bool> drawAction)
        {
            private readonly ScreenRenderTarget _renderTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.Half);
            private readonly Func<IReadOnlyList<int>, bool> _drawAction = drawAction;

            private bool _canBeDrawnToScreen = false;

            public void RenderToTarget(IReadOnlyList<int> projectiles)
            {
                if (projectiles.Count == 0)
                    return;

                var spriteBatchSpanshot = new SpriteBatchSnapshot
                {
                    SortMode = SpriteSortMode.Deferred,
                    BlendState = BlendState.AlphaBlend,
                    SamplerState = Main.DefaultSamplerState,
                    DepthStencilState = DepthStencilState.None,
                    RasterizerState = Main.Rasterizer,
                    Effect = null,
                    Matrix = GameMatrices.Effect * Matrix.CreateScale(0.5f)
                };

                // Требуется для отрисовки примитивов
                // И да, без этого никак...

                var device = Main.graphics.GraphicsDevice;

                device.BlendState = spriteBatchSpanshot.BlendState;
                device.SamplerStates[0] = spriteBatchSpanshot.SamplerState;
                device.DepthStencilState = spriteBatchSpanshot.DepthStencilState;
                device.RasterizerState = spriteBatchSpanshot.RasterizerState;

                device.SetRenderTarget(_renderTarget);
                device.Clear(Color.Transparent);
                {
                    Main.spriteBatch.Begin(spriteBatchSpanshot);
                    _canBeDrawnToScreen = _drawAction(projectiles);
                    Main.spriteBatch.End();
                }
                device.SetRenderTarget(null);
            }

            public void DrawToScreen()
            {
                if (!_canBeDrawnToScreen)
                    return;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, GameMatrices.Zoom);
                Main.spriteBatch.Draw(_renderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                Main.spriteBatch.End();

                // Костыль, исправляющий проблему с отрисовкой трейлов Зенита, Радужного жезла, да и скорее всего других модовых трейлов, если они рисуют их как ванилка...
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, GameMatrices.Transform);
                Main.spriteBatch.End();

                _canBeDrawnToScreen = false;
            }
        }
    }
}