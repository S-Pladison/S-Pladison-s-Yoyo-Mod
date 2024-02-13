using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Rendering;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.PixelatedLayers
{
    // Based on https://github.com/ProjectStarlight/StarlightRiver/blob/master/Core/Systems/PixelationSystem/PixelationSystem.cs

    [Autoload(Side = ModSide.Client)]
    public class PixelatedDrawLayers : ILoadable
    {
        [Autoload(false)]
        private class PixelatedRenderTargetContent : RenderTargetContent
        {
            public override string Name { get => $"Pixelated{Layer}RenderTargetContent"; }
            public override Point Size { get => new(Main.screenWidth / 2, Main.screenHeight / 2); }
            public PixelatedLayer Layer { get; init; }

            public event Action OnDrawToTarget;

            public PixelatedRenderTargetContent(PixelatedLayer layer)
            {
                Layer = layer;
            }

            public override bool PreRender()
            {
                return OnDrawToTarget is not null;
            }

            public override void DrawToTarget()
            {
                var spriteBatchSpanshot = new SpriteBatchSnapshot
                {
                    SortMode = SpriteSortMode.Deferred,
                    BlendState = BlendState.AlphaBlend,
                    SamplerState = Main.DefaultSamplerState,
                    DepthStencilState = DepthStencilState.None,
                    RasterizerState = Main.Rasterizer,
                    Effect = null,
                    Matrix = Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix
                };

                PrepareGraphicsDevice(spriteBatchSpanshot);
                Main.spriteBatch.Begin(spriteBatchSpanshot);
                OnDrawToTarget();
                Main.spriteBatch.End();

                OnDrawToTarget = null;
            }

            public void DrawToScreen()
            {
                if (!IsRenderedInThisFrame || !TryGetRenderTarget(out var target)) return;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                Main.spriteBatch.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }

            private void PrepareGraphicsDevice(SpriteBatchSnapshot spriteBatchSnapshot)
            {
                var device = Main.graphics.GraphicsDevice;
                device.BlendState = spriteBatchSnapshot.BlendState;
                device.SamplerStates[0] = spriteBatchSnapshot.SamplerState;
                device.DepthStencilState = spriteBatchSnapshot.DepthStencilState;
                device.RasterizerState = spriteBatchSnapshot.RasterizerState;
            }
        }

        private readonly List<PixelatedRenderTargetContent> renderers;
        private readonly Dictionary<PixelatedLayer, PixelatedRenderTargetContent> rendererByLayerDict;

        public PixelatedDrawLayers()
        {
            renderers = new List<PixelatedRenderTargetContent>();
            rendererByLayerDict = new Dictionary<PixelatedLayer, PixelatedRenderTargetContent>();
        }

        void ILoadable.Load(Mod mod)
        {
            foreach (var layer in Enum.GetValues(typeof(PixelatedLayer)) as PixelatedLayer[])
            {
                var renderer = new PixelatedRenderTargetContent(layer);

                renderers.Add(renderer);
                rendererByLayerDict.Add(layer, renderer);

                mod.AddContent(renderer);
            }

            ModEvents.OnPostDrawTiles += rendererByLayerDict[PixelatedLayer.OverSolidTiles].DrawToScreen;

            On_Main.DrawCachedProjs += (orig, main, projCache, startSpriteBatch) =>
            {
                if (projCache == Main.instance.DrawCacheProjsBehindProjectiles)
                    rendererByLayerDict[PixelatedLayer.UnderProjectiles].DrawToScreen();

                orig(main, projCache, startSpriteBatch);

                if (projCache == Main.instance.DrawCacheProjsOverPlayers)
                    rendererByLayerDict[PixelatedLayer.OverProjectiles].DrawToScreen();
            };

            ModEvents.OnPostDrawDust += rendererByLayerDict[PixelatedLayer.OverDusts].DrawToScreen;
        }

        void ILoadable.Unload() { }

        public void QueueDrawAction(PixelatedLayer layer, Action action)
        {
            rendererByLayerDict[layer].OnDrawToTarget += action;
        }
    }
}