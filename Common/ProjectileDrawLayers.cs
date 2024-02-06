using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using SPYoyoMod.Utils.Extensions;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    /// <summary>
    /// Class responsible for drawing additional layers of projectiles.
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public sealed class ProjectileDrawLayers : ILoadable
    {
        /// <summary>
        /// Transform matrices required for rendering primitives. Similar to <see cref="Main"/>.GameViewMatrix.TransformationMatrix, but for primitives, not sprites.
        /// </summary>
        public static PrimitiveMatrices DefaultPrimitiveMatrices { get; private set; }

        /// <summary>
        /// Transform matrices required for rendering pixelated primitives.
        /// </summary>
        public static PrimitiveMatrices PixelatedPrimitiveMatrices { get; private set; }

        private Matrix projectionMatrix;

        void ILoadable.Load(Mod mod)
        {
            projectionMatrix = new Matrix(0f, 0f, 0f, 0f,
                                           0f, 0f, 0f, 0f,
                                           0f, 0f, 1f, 0f,
                                          -1f, 1f, 0f, 1f);

            On_Main.DoDraw_UpdateCameraPosition += (orig) =>
            {
                orig();

                UpdateMatrices();
            };

            On_Main.DrawCachedProjs += (orig, main, projCache, startSpriteBatch) =>
            {
                var projectiles = DrawUtils.GetActiveForDrawProjectiles();

                if (projCache == Main.instance.DrawCacheProjsBehindProjectiles)
                    PreDrawProjectiles(ref Main.spriteBatch, projectiles);

                orig(main, projCache, startSpriteBatch);

                if (projCache == Main.instance.DrawCacheProjsOverPlayers)
                    PostDrawProjectiles(ref Main.spriteBatch, projectiles);
            };
        }

        void ILoadable.Unload() { }

        private void UpdateMatrices()
        {
            var viewport = Main.graphics.GraphicsDevice.Viewport;
            var screenOffsetMatrix = Matrix.CreateTranslation(new Vector3(-Main.screenPosition, 0));

            projectionMatrix.M11 = 2f / viewport.Width;
            projectionMatrix.M22 = -2f / viewport.Height;
            projectionMatrix.M41 = -1f - 0.5f * projectionMatrix.M11;
            projectionMatrix.M42 = 1f - 0.5f * projectionMatrix.M22;

            var transformMatrix = Main.GameViewMatrix.TransformationMatrix;
            var transformWithScreenOffsetMatrix = transformMatrix;
            var pixelatedTransformMatrix = Main.GameViewMatrix.EffectMatrix;
            var pixelatedTransformWithScreenOffsetMatrix = pixelatedTransformMatrix;

            Matrix.Multiply(ref screenOffsetMatrix, ref transformWithScreenOffsetMatrix, out transformWithScreenOffsetMatrix);
            Matrix.Multiply(ref screenOffsetMatrix, ref pixelatedTransformWithScreenOffsetMatrix, out pixelatedTransformWithScreenOffsetMatrix);

            Matrix.Multiply(ref transformMatrix, ref projectionMatrix, out transformMatrix);
            Matrix.Multiply(ref transformWithScreenOffsetMatrix, ref projectionMatrix, out transformWithScreenOffsetMatrix);

            DefaultPrimitiveMatrices = new PrimitiveMatrices(transformMatrix, transformWithScreenOffsetMatrix);

            Matrix.Multiply(ref pixelatedTransformMatrix, ref projectionMatrix, out pixelatedTransformMatrix);
            Matrix.Multiply(ref pixelatedTransformWithScreenOffsetMatrix, ref projectionMatrix, out pixelatedTransformWithScreenOffsetMatrix);

            PixelatedPrimitiveMatrices = new PrimitiveMatrices(pixelatedTransformMatrix, pixelatedTransformWithScreenOffsetMatrix);
        }

        private static void PreDrawProjectiles(ref SpriteBatch sb, IReadOnlyList<Projectile> projectiles)
        {
            var projIndex = IPreDrawAdditiveProjectile.FirstProjIndex(projectiles);

            if (projIndex >= 0)
            {
                var spriteBatchSpanshot = new SpriteBatchSnapshot
                {
                    SortMode = SpriteSortMode.Deferred,
                    BlendState = BlendState.Additive,
                    SamplerState = Main.DefaultSamplerState,
                    DepthStencilState = DepthStencilState.None,
                    RasterizerState = Main.Rasterizer,
                    Effect = null,
                    Matrix = Main.GameViewMatrix.TransformationMatrix
                };

                ResetGraphicsDevice(spriteBatchSpanshot);
                Main.spriteBatch.Begin(spriteBatchSpanshot);
                IPreDrawAdditiveProjectile.DrawProjs(projectiles, projIndex);
                Main.spriteBatch.End();
            }

            var rtContent = ModContent.GetInstance<PreDrawPixelatedRenderTargetContent>();

            if (rtContent.IsRenderedInThisFrame && rtContent.TryGetRenderTarget(out RenderTarget2D target))
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                sb.End();
            }
        }

        private static void PostDrawProjectiles(ref SpriteBatch sb, IReadOnlyList<Projectile> projectiles)
        {
            var rtContent = ModContent.GetInstance<PostDrawPixelatedRenderTargetContent>();

            if (rtContent.IsRenderedInThisFrame && rtContent.TryGetRenderTarget(out RenderTarget2D target))
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                sb.End();
            }

            var projIndex = IPostDrawAdditiveProjectile.FirstProjIndex(projectiles);

            if (projIndex >= 0)
            {
                var spriteBatchSpanshot = new SpriteBatchSnapshot
                {
                    SortMode = SpriteSortMode.Deferred,
                    BlendState = BlendState.Additive,
                    SamplerState = Main.DefaultSamplerState,
                    DepthStencilState = DepthStencilState.None,
                    RasterizerState = Main.Rasterizer,
                    Effect = null,
                    Matrix = Main.GameViewMatrix.TransformationMatrix
                };

                ResetGraphicsDevice(spriteBatchSpanshot);
                Main.spriteBatch.Begin(spriteBatchSpanshot);
                IPostDrawAdditiveProjectile.DrawProjs(projectiles, projIndex);
                Main.spriteBatch.End();
            }
        }

        private static void ResetGraphicsDevice(SpriteBatchSnapshot spriteBatchSnapshot)
        {
            var device = Main.graphics.GraphicsDevice;
            device.BlendState = spriteBatchSnapshot.BlendState;
            device.SamplerStates[0] = spriteBatchSnapshot.SamplerState;
            device.DepthStencilState = spriteBatchSnapshot.DepthStencilState;
            device.RasterizerState = spriteBatchSnapshot.RasterizerState;
        }

        private abstract class PixelatedRenderTargetContent : RenderTargetContent
        {
            public override Point Size { get => new(Main.screenWidth / 2, Main.screenHeight / 2); }

            protected int projIndex;
        }

        private class PreDrawPixelatedRenderTargetContent : PixelatedRenderTargetContent
        {
            public override bool PreRender()
            {
                var projectiles = DrawUtils.GetActiveForDrawProjectiles();
                return (projIndex = IPreDrawPixelatedProjectile.FirstProjIndex(projectiles)) > 0;
            }

            public override void DrawToTarget()
            {
                var projectiles = DrawUtils.GetActiveForDrawProjectiles();
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

                ResetGraphicsDevice(spriteBatchSpanshot);
                Main.spriteBatch.Begin(spriteBatchSpanshot);
                IPreDrawPixelatedProjectile.DrawProjs(projectiles, projIndex);
                Main.spriteBatch.End();
            }
        }

        private class PostDrawPixelatedRenderTargetContent : PixelatedRenderTargetContent
        {
            public override bool PreRender()
            {
                var projectiles = DrawUtils.GetActiveForDrawProjectiles();
                return (projIndex = IPostDrawPixelatedProjectile.FirstProjIndex(projectiles)) > 0;
            }

            public override void DrawToTarget()
            {
                var projectiles = DrawUtils.GetActiveForDrawProjectiles();
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

                ResetGraphicsDevice(spriteBatchSpanshot);
                Main.spriteBatch.Begin(spriteBatchSpanshot);
                IPostDrawPixelatedProjectile.DrawProjs(projectiles, projIndex);
                Main.spriteBatch.End();
            }
        }
    }
}