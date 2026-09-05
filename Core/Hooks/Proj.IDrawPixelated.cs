using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Core.Graphics.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System;
using System.Collections.Generic;
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
        /// Слой совпадает со слоем самого снаряда, включая <see cref="Projectile.hide"/>.
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
        /// Слой совпадает со слоем самого снаряда, включая <see cref="Projectile.hide"/>.
        /// </summary>
        void PostDrawPixelated(Projectile proj);
    }

    /// <summary>
    /// Позволяет снаряду отрисовывать пикселизированные эффекты.
    /// Слой совпадает со слоем самого снаряда, включая <see cref="Projectile.hide"/>.
    /// <br/>Интерфейс относится к следующим классам: <see cref="ModProjectile"/> и <see cref="GlobalProjectile"/>
    /// </summary>
    public interface IDrawPixelatedProjectile : IPreHook, IPostHook
    {
        [Autoload(Side = ModSide.Client)]
        private sealed class DrawPixelatedProjectileImplementation : ModSystem
        {
            private enum Layer
            {
                BehindNPCsAndTiles,
                BehindNPCs,
                BehindProjectiles,
                Projectiles,
                Held,
                OverPlayers,
                OverWiresUI
            }

            private static readonly Layer[] _allLayers = Enum.GetValues<Layer>();
            private static readonly List<int>[] _preProjectiles = CreateLayerLists();
            private static readonly List<int>[] _postProjectiles = CreateLayerLists();

            private static readonly List<int> _hideBehindNPCsAndTiles = [];
            private static readonly List<int> _hideBehindNPCs = [];
            private static readonly List<int> _hideBehindProjectiles = [];
            private static readonly List<int> _hideOverPlayers = [];
            private static readonly List<int> _hideOverWiresUI = [];

            private static ProjectilePixelatedLayer[] _preLayers;
            private static ProjectilePixelatedLayer[] _postLayers;

            private static bool[] _preDrawByType;
            private static bool[] _postDrawByType;
            private static bool _anyPreGlobal;
            private static bool _anyPostGlobal;
            private static bool _hasHideLayerContent;
            private static bool _typeCacheBuilt;

            public override void Load()
            {
                _preLayers = new ProjectilePixelatedLayer[_allLayers.Length];
                _postLayers = new ProjectilePixelatedLayer[_allLayers.Length];

                for (var i = 0; i < _allLayers.Length; i++)
                {
                    _preLayers[i] = new(DrawPrePixelatedProjectiles);
                    _postLayers[i] = new(DrawPostPixelatedProjectiles);
                }

                ModEvents.OnPostUpdateCameraPosition += RenderProjectilesToTargets;

                On_Main.DrawCachedProjs += DrawCachedProjs;
                On_Main.DrawProjectiles += DrawProjectiles;
                On_Main.DrawPlayers_AfterProjectiles += DrawPlayersAfterProjectiles;
            }

            public override void PostSetupContent()
                => BuildTypeCache();

            public override void Unload()
            {
                On_Main.DrawPlayers_AfterProjectiles -= DrawPlayersAfterProjectiles;
                On_Main.DrawProjectiles -= DrawProjectiles;
                On_Main.DrawCachedProjs -= DrawCachedProjs;

                ModEvents.OnPostUpdateCameraPosition -= RenderProjectilesToTargets;

                _postLayers = null;
                _preLayers = null;
                _postDrawByType = null;
                _preDrawByType = null;
                _typeCacheBuilt = false;
            }

            private static List<int>[] CreateLayerLists()
            {
                var lists = new List<int>[_allLayers.Length];

                for (var i = 0; i < _allLayers.Length; i++)
                    lists[i] = new List<int>(32);

                return lists;
            }

            private static void ClearLayerLists()
            {
                for (var i = 0; i < _allLayers.Length; i++)
                {
                    _preProjectiles[i].Clear();
                    _postProjectiles[i].Clear();
                }
            }

            private static void BuildTypeCache()
            {
                var count = ProjectileLoader.ProjectileCount;
                var preByType = new bool[count];
                var postByType = new bool[count];
                var preGlobals = new List<GlobalProjectile>();
                var postGlobals = new List<GlobalProjectile>();

                for (var type = 0; type < count; type++)
                {
                    var modProj = ProjectileLoader.GetProjectile(type);
                    if (modProj is null)
                        continue;

                    if (modProj is IPreHook)
                        preByType[type] = true;

                    if (modProj is IPostHook)
                        postByType[type] = true;
                }

                foreach (var global in ModContent.GetContent<GlobalProjectile>())
                {
                    if (global is IPreHook)
                        preGlobals.Add(global);

                    if (global is IPostHook)
                        postGlobals.Add(global);
                }

                ApplyGlobalsByType(preByType, preGlobals);
                ApplyGlobalsByType(postByType, postGlobals);

                _preDrawByType = preByType;
                _postDrawByType = postByType;
                _anyPreGlobal = preGlobals.Count > 0;
                _anyPostGlobal = postGlobals.Count > 0;
                _typeCacheBuilt = true;
            }

            private static void ApplyGlobalsByType(bool[] byType, List<GlobalProjectile> globals)
            {
                if (globals.Count == 0)
                    return;

                foreach (var global in globals)
                {
                    if (global.ConditionallyAppliesToEntities)
                        continue;

                    Array.Fill(byType, true);
                    return;
                }

                for (var type = 0; type < byType.Length; type++)
                {
                    if (byType[type])
                        continue;

                    if (!TryGetSampleProjectile(type, out var sample))
                        continue;

                    foreach (var global in globals)
                    {
                        if (AppliesToProjectile(global, sample))
                        {
                            byType[type] = true;
                            break;
                        }
                    }
                }
            }

            private static bool TryGetSampleProjectile(int type, out Projectile sample)
            {
                if (ContentSamples.ProjectilesByType.TryGetValue(type, out sample))
                    return true;

                sample = ProjectileLoader.GetProjectile(type)?.Projectile;
                return sample is not null;
            }

            private static bool AppliesToProjectile(GlobalProjectile global, Projectile proj)
                => global.AppliesToEntity(proj, lateInstantiation: false)
                || global.AppliesToEntity(proj, lateInstantiation: true);

            private static bool IsHeld(int projIndex)
            {
                foreach (var player in Main.ActivePlayers)
                {
                    if (player.heldProj == projIndex)
                        return true;
                }

                return false;
            }

            private static bool IsOnScreen(Projectile proj)
            {
                var fluff = ProjectileID.Sets.DrawScreenCheckFluff[proj.type];
                var cameraPos = Main.Camera.ScaledPosition;
                var cameraSize = Main.Camera.ScaledSize;
                var visibleRectangle = new Rectangle(
                    (int)cameraPos.X - fluff,
                    (int)cameraPos.Y - fluff,
                    (int)cameraSize.X + fluff * 2,
                    (int)cameraSize.Y + fluff * 2
                );

                return visibleRectangle.Intersects(proj.Hitbox);
            }

            private static bool TryGetPixelatedHooks(Projectile proj, out bool hasPre, out bool hasPost)
            {
                var type = proj.type;

                hasPre = _preDrawByType[type];
                hasPost = _postDrawByType[type];

                return hasPre || hasPost;
            }

            private static void AddToLayer(Layer layer, int projIndex, bool hasPre, bool hasPost)
            {
                var index = (int)layer;

                if (hasPre)
                    _preProjectiles[index].Add(projIndex);

                if (hasPost)
                    _postProjectiles[index].Add(projIndex);

                if (layer is not Layer.Projectiles and not Layer.Held)
                    _hasHideLayerContent = true;
            }

            private static void CollectHideLayers(Projectile proj, bool hasPre, bool hasPost)
            {
                _hideBehindNPCsAndTiles.Clear();
                _hideBehindNPCs.Clear();
                _hideBehindProjectiles.Clear();
                _hideOverPlayers.Clear();
                _hideOverWiresUI.Clear();

                proj.ModProjectile?.DrawBehind(
                    proj.whoAmI,
                    _hideBehindNPCsAndTiles,
                    _hideBehindNPCs,
                    _hideBehindProjectiles,
                    _hideOverPlayers,
                    _hideOverWiresUI
                );

                foreach (var global in proj.Globals)
                {
                    global.DrawBehind(
                        proj,
                        proj.whoAmI,
                        _hideBehindNPCsAndTiles,
                        _hideBehindNPCs,
                        _hideBehindProjectiles,
                        _hideOverPlayers,
                        _hideOverWiresUI
                    );
                }

                var whoAmI = proj.whoAmI;

                if (_hideBehindNPCsAndTiles.Contains(whoAmI))
                    AddToLayer(Layer.BehindNPCsAndTiles, whoAmI, hasPre, hasPost);

                if (_hideBehindNPCs.Contains(whoAmI))
                    AddToLayer(Layer.BehindNPCs, whoAmI, hasPre, hasPost);

                if (_hideBehindProjectiles.Contains(whoAmI))
                    AddToLayer(Layer.BehindProjectiles, whoAmI, hasPre, hasPost);

                if (_hideOverPlayers.Contains(whoAmI))
                    AddToLayer(Layer.OverPlayers, whoAmI, hasPre, hasPost);

                if (_hideOverWiresUI.Contains(whoAmI))
                    AddToLayer(Layer.OverWiresUI, whoAmI, hasPre, hasPost);
            }

            private static void CollectProjectiles()
            {
                if (!_typeCacheBuilt)
                    return;

                ClearLayerLists();
                _hasHideLayerContent = false;

                foreach (var proj in Main.ActiveProjectiles)
                {
                    if (!TryGetPixelatedHooks(proj, out var hasPre, out var hasPost))
                        continue;

                    if (!IsOnScreen(proj))
                        continue;

                    if (IsHeld(proj.whoAmI))
                    {
                        AddToLayer(Layer.Held, proj.whoAmI, hasPre, hasPost);
                        continue;
                    }

                    if (!proj.hide)
                    {
                        AddToLayer(Layer.Projectiles, proj.whoAmI, hasPre, hasPost);
                        continue;
                    }

                    CollectHideLayers(proj, hasPre, hasPost);
                }
            }

            private static void RenderProjectilesToTargets()
            {
                CollectProjectiles();

                for (var i = 0; i < _allLayers.Length; i++)
                {
                    _preLayers[i].RenderToTarget(_preProjectiles[i]);
                    _postLayers[i].RenderToTarget(_postProjectiles[i]);
                }
            }

            private static bool DrawPrePixelatedProjectiles(IReadOnlyList<int> projs)
                => InvokePixelatedDraw(projs, pre: true);

            private static bool DrawPostPixelatedProjectiles(IReadOnlyList<int> projs)
                => InvokePixelatedDraw(projs, pre: false);

            private static bool InvokePixelatedDraw(IReadOnlyList<int> projs, bool pre)
            {
                var anyDrawCalls = false;

                for (int i = 0, count = projs.Count; i < count; i++)
                {
                    ref var proj = ref Main.projectile[projs[i]];

                    if (!proj.active)
                        continue;

                    try
                    {
                        if (pre)
                        {
                            if (proj.ModProjectile is IPreHook modProj)
                            {
                                modProj.PreDrawPixelated(proj);
                                anyDrawCalls = true;
                            }

                            if (_anyPreGlobal)
                            {
                                foreach (IPreHook g in IPreHook._hook.Enumerate(proj))
                                {
                                    g.PreDrawPixelated(proj);
                                    anyDrawCalls = true;
                                }
                            }
                        }
                        else
                        {
                            if (proj.ModProjectile is IPostHook modProj)
                            {
                                modProj.PostDrawPixelated(proj);
                                anyDrawCalls = true;
                            }

                            if (_anyPostGlobal)
                            {
                                foreach (IPostHook g in IPostHook._hook.Enumerate(proj))
                                {
                                    g.PostDrawPixelated(proj);
                                    anyDrawCalls = true;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        proj.active = false;
                    }
                }

                return anyDrawCalls;
            }

            private static bool TryGetCachedLayer(List<int> projCache, out Layer layer)
            {
                var main = Main.instance;

                if (ReferenceEquals(projCache, main.DrawCacheProjsBehindNPCsAndTiles))
                {
                    layer = Layer.BehindNPCsAndTiles;
                    return true;
                }

                if (ReferenceEquals(projCache, main.DrawCacheProjsBehindNPCs))
                {
                    layer = Layer.BehindNPCs;
                    return true;
                }

                if (ReferenceEquals(projCache, main.DrawCacheProjsBehindProjectiles))
                {
                    layer = Layer.BehindProjectiles;
                    return true;
                }

                if (ReferenceEquals(projCache, main.DrawCacheProjsOverPlayers))
                {
                    layer = Layer.OverPlayers;
                    return true;
                }

                if (ReferenceEquals(projCache, main.DrawCacheProjsOverWiresUI))
                {
                    layer = Layer.OverWiresUI;
                    return true;
                }

                layer = default;
                return false;
            }

            private static void DrawLayerPair(Layer layer, Action drawVanilla)
            {
                _preLayers[(int)layer].DrawToScreen();
                drawVanilla();
                _postLayers[(int)layer].DrawToScreen();
            }

            private static void DrawCachedProjs(On_Main.orig_DrawCachedProjs orig, Main main, List<int> projCache, bool startSpriteBatch)
            {
                if (!_hasHideLayerContent)
                {
                    orig(main, projCache, startSpriteBatch);
                    return;
                }

                if (!TryGetCachedLayer(projCache, out var layer))
                {
                    orig(main, projCache, startSpriteBatch);
                    return;
                }

                var pre = _preLayers[(int)layer];
                var post = _postLayers[(int)layer];

                if (startSpriteBatch)
                {
                    pre.DrawToScreen();
                    orig(main, projCache, startSpriteBatch);
                    post.DrawToScreen();
                    return;
                }

                if (!pre.CanBeDrawn && !post.CanBeDrawn)
                {
                    orig(main, projCache, startSpriteBatch);
                    return;
                }

                Main.spriteBatch.End(out var snapshot);
                pre.DrawToScreen();
                Main.spriteBatch.Begin(snapshot);

                orig(main, projCache, startSpriteBatch);

                Main.spriteBatch.End();
                post.DrawToScreen();
                Main.spriteBatch.Begin(snapshot);
            }

            private static void DrawProjectiles(On_Main.orig_DrawProjectiles orig, Main main)
                => DrawLayerPair(Layer.Projectiles, () => orig(main));

            private static void DrawPlayersAfterProjectiles(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main main)
                => DrawLayerPair(Layer.Held, () => orig(main));
        }

        private sealed class ProjectilePixelatedLayer(Func<IReadOnlyList<int>, bool> drawAction)
        {
            private readonly ScreenRenderTarget _renderTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.Half);
            private readonly Func<IReadOnlyList<int>, bool> _drawAction = drawAction;

            private bool _canBeDrawnToScreen;

            public bool CanBeDrawn => _canBeDrawnToScreen;

            public void RenderToTarget(IReadOnlyList<int> projectiles)
            {
                _canBeDrawnToScreen = false;

                if (projectiles.Count == 0)
                    return;

                var spriteBatchSnapshot = new SpriteBatchSnapshot
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

                device.BlendState = spriteBatchSnapshot.BlendState;
                device.SamplerStates.Set(spriteBatchSnapshot.SamplerState);
                device.DepthStencilState = spriteBatchSnapshot.DepthStencilState;
                device.RasterizerState = spriteBatchSnapshot.RasterizerState;

                device.SetRenderTarget(_renderTarget);
                device.Clear(Color.Transparent);
                {
                    Main.spriteBatch.Begin(spriteBatchSnapshot);
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