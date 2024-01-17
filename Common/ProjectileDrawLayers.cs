﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.RenderTargets;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public class ProjectileDrawLayers : ILoadable
    {
        public Matrix TransformMatrix { get; private set; }

        void ILoadable.Load(Mod mod)
        {
            On_Main.DoDraw_UpdateCameraPosition += (orig) =>
            {
                orig();

                TransformMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);
                TransformMatrix *= Main.GameViewMatrix.EffectMatrix;
                TransformMatrix *= Matrix.CreateTranslation(Main.screenWidth / 2, Main.screenHeight / -2, 0);
                TransformMatrix *= Matrix.CreateRotationZ(MathHelper.Pi);
                TransformMatrix *= Matrix.CreateScale(Main.GameViewMatrix.Zoom.X, Main.GameViewMatrix.Zoom.Y, 1f);
                TransformMatrix *= Matrix.CreateOrthographic(Main.screenWidth, Main.screenHeight, 0, 1000);
            };

            On_Main.DrawCachedProjs += (orig, main, projCache, startSpriteBatch) =>
            {
                ref var sb = ref Main.spriteBatch;

                if (projCache == Main.instance.DrawCacheProjsBehindProjectiles)
                {
                    if (ModContent.GetInstance<PreDrawPixelizationRenderTargetContent>().TryGetRenderTarget(out RenderTarget2D target))
                    {
                        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                        sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                        sb.End();
                    }

                    IPreDrawPrimitivesProjectile.Invoke(TransformMatrix);
                }

                orig(main, projCache, startSpriteBatch);

                if (projCache == Main.instance.DrawCacheProjsOverPlayers)
                {
                    IPostDrawPrimitivesProjectile.Invoke(TransformMatrix);

                    if (ModContent.GetInstance<PostDrawPixelizationRenderTargetContent>().TryGetRenderTarget(out RenderTarget2D target))
                    {
                        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                        sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                        sb.End();
                    }
                }
            };
        }

        void ILoadable.Unload() { }

        private class PreDrawPixelizationRenderTargetContent : RenderTargetContent
        {
            public override bool Active { get => true; }
            public override Point Size { get => new(Main.screenWidth / 2, Main.screenHeight / 2); }

            public override void DrawToTarget()
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix);

                IPreDrawPixelatedProjectile.Invoke();

                Main.spriteBatch.End();
            }
        }

        private class PostDrawPixelizationRenderTargetContent : RenderTargetContent
        {
            public override bool Active { get => true; }
            public override Point Size { get => new(Main.screenWidth / 2, Main.screenHeight / 2); }

            public override void DrawToTarget()
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix);

                IPostDrawPixelatedProjectile.Invoke();

                Main.spriteBatch.End();
            }
        }
    }
}
