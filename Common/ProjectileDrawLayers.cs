using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils.DataStructures;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    [Autoload(Side = ModSide.Client)]
    public class ProjectileDrawLayers : ILoadable
    {
        private Matrix transformMatrix;
        private Matrix transformWithoutScreenOffsetMatrix;
        private Matrix pixelatedTransformMatrix;
        private Matrix pixelatedTransformWithoutScreenOffsetMatrix;

        void ILoadable.Load(Mod mod)
        {
            On_Main.DoDraw_UpdateCameraPosition += (orig) =>
            {
                orig();
                UpdateMatrices();
            };

            On_Main.DrawCachedProjs += (orig, main, projCache, startSpriteBatch) =>
            {
                orig(main, projCache, startSpriteBatch);

                if (projCache == Main.instance.DrawCacheProjsOverPlayers)
                    PostDrawProjectiles(ref Main.spriteBatch);
            };
        }

        void ILoadable.Unload() { }

        private void UpdateMatrices()
        {
            var matrix = Matrix.CreateTranslation(new Vector3(-Main.screenPosition, 0));

            transformWithoutScreenOffsetMatrix = matrix;
            pixelatedTransformWithoutScreenOffsetMatrix = matrix;

            matrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);
            matrix *= Main.GameViewMatrix.EffectMatrix;
            matrix *= Matrix.CreateTranslation(Main.screenWidth / 2, Main.screenHeight / -2, 0);
            matrix *= Matrix.CreateRotationZ(MathHelper.Pi);

            transformMatrix = matrix;
            pixelatedTransformMatrix = matrix;
            transformWithoutScreenOffsetMatrix *= matrix;
            pixelatedTransformWithoutScreenOffsetMatrix *= matrix;

            matrix = Matrix.CreateScale(Main.GameViewMatrix.Zoom.X, Main.GameViewMatrix.Zoom.Y, 1f);

            transformMatrix *= matrix;
            transformWithoutScreenOffsetMatrix *= matrix;

            matrix = Matrix.CreateOrthographic(Main.screenWidth, Main.screenHeight, 0, 1000);

            transformMatrix = matrix;
            pixelatedTransformMatrix = matrix;
            transformWithoutScreenOffsetMatrix *= matrix;
            pixelatedTransformWithoutScreenOffsetMatrix *= matrix;
        }

        public void PostDrawProjectiles(ref SpriteBatch sb)
        {
            IDrawPrimitivesProjectile.Draw(new PrimitiveMatrices(transformMatrix, transformWithoutScreenOffsetMatrix));

            if (ModContent.GetInstance<DrawPixelizationRenderTargetContent>().TryGetRenderTarget(out RenderTarget2D target))
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                sb.End();
            }
        }

        private class DrawPixelizationRenderTargetContent : RenderTargetContent
        {
            public override bool Active { get => true; }
            public override Point Size { get => new(Main.screenWidth / 2, Main.screenHeight / 2); }

            public override void DrawToTarget()
            {
                var projDrawLayerInstance = ModContent.GetInstance<ProjectileDrawLayers>();

                IDrawPixelatedPrimitivesProjectile.Draw(new PrimitiveMatrices(projDrawLayerInstance.pixelatedTransformMatrix, projDrawLayerInstance.pixelatedTransformWithoutScreenOffsetMatrix));
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix);
                IDrawPixelatedProjectile.Draw();
                Main.spriteBatch.End();
            }
        }
    }
}