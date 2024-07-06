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

            ModEvents.OnPostUpdateCameraPosition += PrepareLayers;
        }

        public void Unload()
        {   
            ModEvents.OnPostUpdateCameraPosition -= PrepareLayers;
            
            PostDrawPlayers_AfterProjectiles = null;
            PostDrawProjectiles = null;
            PreDrawProjectiles = null;
        }

        private static void PrepareLayers()
        {
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

            PreDrawProjectiles.SetProjectiles(notHiddenProjs);
            PostDrawProjectiles.SetProjectiles(notHiddenProjs.Except(playerHeldProjs));
            PostDrawPlayers_AfterProjectiles.SetProjectiles(playerHeldProjs);
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
            private IEnumerable<int> _projectiles;
            private bool _wasRendered;

            public override string Name => $"{base.Name}_{_layerName}";

            public PixelatedProjectileDrawLayer(string name, Position position, Action<IEnumerable<int>> drawAction)
            {
                _renderTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.TwiceSmaller);
                _layerName = name;
                _position = position;
                _drawAction = drawAction;
                _projectiles = null;
            }

            public override void Load()
                => ModEvents.OnPostUpdateCameraPosition += Render;

            public override void Unload()
                => ModEvents.OnPostUpdateCameraPosition -= Render;

            public override Position GetDefaultPosition()
                => _position;

            public override bool GetDefaultVisibility()
                => true;

            public void SetProjectiles(IEnumerable<int> projectiles)
                => _projectiles = projectiles;

            public void Render()
            {
                _wasRendered = false;

                if (_projectiles == null || !_projectiles.Any())
                    return;

                Main.graphics.GraphicsDevice.SetRenderTarget(_renderTarget);
                Main.graphics.GraphicsDevice.Clear(Color.Transparent);
                {
                    var spriteBatchSpanshot = new SpriteBatchSnapshot
                    {
                        SortMode = SpriteSortMode.Deferred,
                        BlendState = BlendState.AlphaBlend,
                        SamplerState = Main.DefaultSamplerState,
                        DepthStencilState = DepthStencilState.None,
                        RasterizerState = RasterizerState.CullCounterClockwise,
                        Effect = null,
                        Matrix = Matrix.CreateScale(0.5f)
                    };

                    Main.spriteBatch.Begin(spriteBatchSpanshot);
                    _drawAction(_projectiles);
                    Main.spriteBatch.End();

                }
                Main.graphics.GraphicsDevice.SetRenderTarget(null);

                _projectiles = null;            
                _wasRendered = true;
            }

            protected override void Draw()
            {
                if (!_wasRendered)
                    return;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null,  Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(_renderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }
        }
    }
} 