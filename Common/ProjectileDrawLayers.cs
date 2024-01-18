using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.RenderTargets;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    [Autoload(Side = ModSide.Client)]
    public class ProjectileDrawLayers : ILoadable
    {
        public Matrix TransformMatrix { get; private set; }
        public Matrix PixelatedTransformMatrix { get; private set; }

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
            var matrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);
            matrix *= Main.GameViewMatrix.EffectMatrix;
            matrix *= Matrix.CreateTranslation(Main.screenWidth / 2, Main.screenHeight / -2, 0);
            matrix *= Matrix.CreateRotationZ(MathHelper.Pi);

            PixelatedTransformMatrix = matrix;
            TransformMatrix = matrix;
            TransformMatrix *= Matrix.CreateScale(Main.GameViewMatrix.Zoom.X, Main.GameViewMatrix.Zoom.Y, 1f);

            matrix = Matrix.CreateOrthographic(Main.screenWidth, Main.screenHeight, 0, 1000);

            PixelatedTransformMatrix *= matrix;
            TransformMatrix *= matrix;
        }

        public void PostDrawProjectiles(ref SpriteBatch sb)
        {
            IDrawPrimitivesProjectile.Draw(TransformMatrix);

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
                IDrawPixelatedPrimitivesProjectile.Draw(ModContent.GetInstance<ProjectileDrawLayers>().PixelatedTransformMatrix);
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix);
                IDrawPixelatedProjectile.Draw();
                Main.spriteBatch.End();
            }
        }
    }
}