using System;
using Terraria;
using Terraria.ModLoader;
using IPreHook = SPYoyoMod.Common.Hooks.IPreDrawPixelatedProjectile;
using IPostHook = SPYoyoMod.Common.Hooks.IPostDrawPixelatedProjectile;
using Terraria.ModLoader.Core;
using SPYoyoMod.Common.Graphics.DrawLayers;
using SPYoyoMod.Common.Graphics.RenderTargets;
using Microsoft.Xna.Framework;
using SPYoyoMod.Utils;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace SPYoyoMod.Common.Hooks
{
    /// <summary>
    /// Позволяет снаряду отрисовывать пикселизированные эффекты.
    /// <br/>Интерфейс относится к следующим классам: <see cref="ModProjectile"/> и <see cref="GlobalProjectile"/>
    /// </summary>
    public interface IDrawPixelatedProjectile : IPreHook, IPostHook { }

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

    // Примечание: Данная реализация не учитывает отрисовку скрытых снарядов (proj.hide)
    // Ясное дело, добавить это не так сложно, но на данный момент это просто не нужно...
    [Autoload(Side = ModSide.Client)]
    internal sealed class DrawPixelatedProjectileImplementation : ILoadable
    {
        public static PixelatedProjectileDrawLayer PreDrawProjectiles { get; private set; }
        public static PixelatedProjectileDrawLayer PostDrawProjectiles { get; private set; }
        public static PixelatedProjectileDrawLayer PostDrawPlayers_AfterProjectiles { get; private set; }

        public void Load(Mod mod)
        {
            mod.AddContent(
                PreDrawProjectiles = new PixelatedProjectileDrawLayer(
                    nameof(PreDrawProjectiles),
                    new GameDrawLayer.BeforeParent(VanillaDrawLayers.DrawProjectiles),
                    PreDrawPixelatedProjectile
                )
            );

            mod.AddContent(
                PostDrawProjectiles = new PixelatedProjectileDrawLayer(
                    nameof(PostDrawProjectiles),
                    new GameDrawLayer.AfterParent(VanillaDrawLayers.DrawProjectiles),
                    PostDrawPixelatedProjectile
                )
            );

            mod.AddContent(
                PostDrawPlayers_AfterProjectiles = new PixelatedProjectileDrawLayer(
                    nameof(PostDrawPlayers_AfterProjectiles),
                    new GameDrawLayer.AfterParent(VanillaDrawLayers.DrawPlayers_AfterProjectiles),
                    PostDrawPixelatedProjectile
                )
            );

            ModEvents.OnPostUpdateCameraPosition += RenderLayers;
        }

        public void Unload()
        {   
            ModEvents.OnPostUpdateCameraPosition -= RenderLayers;
            
            PostDrawPlayers_AfterProjectiles = null;
            PostDrawProjectiles = null;
            PreDrawProjectiles = null;
        }

        private static void RenderLayers()
        {
            PreDrawProjectiles.ResetRenderFlag();
            PostDrawProjectiles.ResetRenderFlag();
            PostDrawPlayers_AfterProjectiles.ResetRenderFlag();

            var playerHeldProjs = new List<int>(Main.player.Length / 4);
            var notHiddenProjs = new List<int>(Main.projectile.Length / 2);

            foreach (var player in Main.ActivePlayers)
            {
                if (player.heldProj >= 0)
                    playerHeldProjs.Add(player.heldProj);
            }

            foreach (var proj in Main.ActiveProjectiles)
            {
                if (!proj.hide)
                    notHiddenProjs.Add(proj.whoAmI);
            }

            PreDrawProjectiles.Render(notHiddenProjs);
            PostDrawProjectiles.Render(notHiddenProjs.Except(playerHeldProjs));
            PostDrawPlayers_AfterProjectiles.Render(playerHeldProjs);
        }

        private static void PreDrawPixelatedProjectile(IEnumerable<int> projs)
        {
            foreach (var projIndex in projs)
            {
                ref var proj = ref Main.projectile[projIndex];

                (proj.ModProjectile as IPreHook)?.PreDrawPixelated(proj);

                foreach (IPreHook g in IPreHook._hook.Enumerate(proj))
                {
                    g.PreDrawPixelated(proj);
                }
            }
        }

        private static void PostDrawPixelatedProjectile(IEnumerable<int> projs)
        {
            foreach (var projIndex in projs)
            {
                ref var proj = ref Main.projectile[projIndex];

                (proj.ModProjectile as IPostHook)?.PostDrawPixelated(proj);

                foreach (IPostHook g in IPostHook._hook.Enumerate(proj))
                {
                    g.PostDrawPixelated(proj);
                }
            }
        }

        [Autoload(false)]
        internal sealed class PixelatedProjectileDrawLayer : GameDrawLayer
        {
            private readonly ScreenRenderTarget _renderTarget;
            private readonly string _layerName;
            private readonly Position _position;
            private readonly Action<IEnumerable<int>> _drawAction;
            private bool _wasRendered;

            public override string Name => $"{base.Name}_{_layerName}";

            public PixelatedProjectileDrawLayer(string name, Position position, Action<IEnumerable<int>> drawAction)
            {
                _renderTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.TwiceSmaller);
                _layerName = name;
                _position = position;
                _drawAction = drawAction;
            }

            public override Position GetDefaultPosition()
                => _position;

            public override bool GetDefaultVisibility()
                => _wasRendered;

            public void ResetRenderFlag()
                => _wasRendered = false;

            public void Render(IEnumerable<int> projectiles)
            {
                if (!projectiles.Any())
                    return;

                var device = Main.graphics.GraphicsDevice;
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
                device.BlendState = spriteBatchSpanshot.BlendState;
                device.SamplerStates[0] = spriteBatchSpanshot.SamplerState;
                device.DepthStencilState = spriteBatchSpanshot.DepthStencilState;
                device.RasterizerState = spriteBatchSpanshot.RasterizerState;

                device.SetRenderTarget(_renderTarget);
                device.Clear(Color.Transparent);
                {
                    Main.spriteBatch.Begin(spriteBatchSpanshot);
                    _drawAction(projectiles);
                    Main.spriteBatch.End();
                }
                device.SetRenderTarget(null);

                _wasRendered = true;
            }

            protected override void Draw()
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, GameMatrices.Zoom);
                Main.spriteBatch.Draw(_renderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }
        }
    }
} 