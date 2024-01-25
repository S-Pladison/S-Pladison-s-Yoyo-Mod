using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    [Autoload(Side = ModSide.Client)]
    public sealed class ProjectileDrawLayers : ILoadable
    {
        private Matrix transformMatrix;
        private Matrix transformWithScreenOffsetMatrix;
        private Matrix pixelatedTransformMatrix;
        private Matrix pixelatedTransformWithScreenOffsetMatrix;

        void ILoadable.Load(Mod mod)
        {
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
            var zoom = Main.GameViewMatrix.Zoom;
            var zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);
            var screenOffsetMatrix = Matrix.CreateTranslation(new Vector3(-Main.screenPosition, 0));

            transformMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);
            transformMatrix *= Matrix.CreateTranslation(0f, -Main.screenHeight, 0f);
            transformMatrix *= Matrix.CreateRotationZ(MathHelper.Pi);
            transformMatrix *= Main.GameViewMatrix.EffectMatrix;
            transformMatrix *= zoomScaleMatrix;
            transformMatrix *= Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth * zoom.X, 0f, Main.screenHeight * zoom.Y, 0f, 1f);
            transformMatrix *= zoomScaleMatrix;

            transformWithScreenOffsetMatrix = screenOffsetMatrix * transformMatrix;

            pixelatedTransformMatrix = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1f, 1f);

            pixelatedTransformWithScreenOffsetMatrix = screenOffsetMatrix * pixelatedTransformMatrix;
        }

        private void PreDrawProjectiles(ref SpriteBatch sb, IReadOnlyList<Projectile> projectiles)
        {
            var rtContent = ModContent.GetInstance<PreDrawPixelatedRenderTargetContent>();

            if (rtContent.IsRenderedInThisFrame && rtContent.TryGetRenderTarget(out RenderTarget2D target))
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                sb.End();
            }

            var prtContent = ModContent.GetInstance<PreDrawPixelatedPrimitivesRenderTargetContent>();

            if (prtContent.IsRenderedInThisFrame && prtContent.TryGetRenderTarget(out target))
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                sb.End();
            }

            var projIndex = IDrawPrimitivesProjectile.FirstProjIndex(projectiles, true);

            if (projIndex >= 0)
            {
                PrepareToDrawPrimitives(Main.GameViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? RasterizerState.CullClockwise : RasterizerState.CullCounterClockwise);
                IDrawPrimitivesProjectile.PreDrawProjs(projectiles, projIndex, new PrimitiveMatrices(transformMatrix, transformWithScreenOffsetMatrix));
            }
        }

        private void PostDrawProjectiles(ref SpriteBatch sb, IReadOnlyList<Projectile> projectiles)
        {
            var projIndex = IDrawPrimitivesProjectile.FirstProjIndex(projectiles, false);

            if (projIndex >= 0)
            {
                PrepareToDrawPrimitives(Main.GameViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? RasterizerState.CullClockwise : RasterizerState.CullCounterClockwise);
                IDrawPrimitivesProjectile.PostDrawProjs(projectiles, projIndex, new PrimitiveMatrices(transformMatrix, transformWithScreenOffsetMatrix));
            }

            var prtContent = ModContent.GetInstance<PostDrawPixelatedPrimitivesRenderTargetContent>();

            if (prtContent.IsRenderedInThisFrame && prtContent.TryGetRenderTarget(out RenderTarget2D target))
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                sb.End();
            }

            var rtContent = ModContent.GetInstance<PostDrawPixelatedRenderTargetContent>();

            if (rtContent.IsRenderedInThisFrame && rtContent.TryGetRenderTarget(out target))
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                sb.End();
            }
        }

        private static void PrepareToDrawPrimitives(RasterizerState rasterizerState)
        {
            Main.graphics.GraphicsDevice.RasterizerState = rasterizerState;
            Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
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
                return (projIndex = IDrawPixelatedProjectile.FirstProjIndex(projectiles, true)) > 0;
            }

            public override void DrawToTarget()
            {
                var projectiles = DrawUtils.GetActiveForDrawProjectiles();

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix);
                IDrawPixelatedProjectile.PreDrawProjs(projectiles, projIndex);
                Main.spriteBatch.End();
            }
        }

        private class PreDrawPixelatedPrimitivesRenderTargetContent : PixelatedRenderTargetContent
        {
            public override bool PreRender()
            {
                var projectiles = DrawUtils.GetActiveForDrawProjectiles();
                return (projIndex = IDrawPixelatedPrimitivesProjectile.FirstProjIndex(projectiles, true)) > 0;
            }

            public override void DrawToTarget()
            {
                var projDrawLayerInstance = ModContent.GetInstance<ProjectileDrawLayers>();
                var projectiles = DrawUtils.GetActiveForDrawProjectiles();
                var matrices = new PrimitiveMatrices(projDrawLayerInstance.pixelatedTransformMatrix, projDrawLayerInstance.pixelatedTransformWithScreenOffsetMatrix);

                PrepareToDrawPrimitives(RasterizerState.CullCounterClockwise);
                IDrawPixelatedPrimitivesProjectile.PreDrawProjs(projectiles, projIndex, matrices);
            }
        }

        private class PostDrawPixelatedRenderTargetContent : PixelatedRenderTargetContent
        {
            public override bool PreRender()
            {
                var projectiles = DrawUtils.GetActiveForDrawProjectiles();
                return (projIndex = IDrawPixelatedProjectile.FirstProjIndex(projectiles, false)) > 0;
            }

            public override void DrawToTarget()
            {
                var projectiles = DrawUtils.GetActiveForDrawProjectiles();

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix);
                IDrawPixelatedProjectile.PostDrawProjs(projectiles, projIndex);
                Main.spriteBatch.End();
            }
        }

        private class PostDrawPixelatedPrimitivesRenderTargetContent : PixelatedRenderTargetContent
        {
            public override bool PreRender()
            {
                var projectiles = DrawUtils.GetActiveForDrawProjectiles();
                return (projIndex = IDrawPixelatedPrimitivesProjectile.FirstProjIndex(projectiles, false)) > 0;
            }

            public override void DrawToTarget()
            {
                var projDrawLayerInstance = ModContent.GetInstance<ProjectileDrawLayers>();
                var projectiles = DrawUtils.GetActiveForDrawProjectiles();
                var matrices = new PrimitiveMatrices(projDrawLayerInstance.pixelatedTransformMatrix, projDrawLayerInstance.pixelatedTransformWithScreenOffsetMatrix);

                PrepareToDrawPrimitives(RasterizerState.CullCounterClockwise);
                IDrawPixelatedPrimitivesProjectile.PostDrawProjs(projectiles, projIndex, matrices);
            }
        }
    }
}