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

        private static Matrix projectionMatrix;

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
                var projectiles = DrawUtils.GetActiveForDrawEntities<Projectile>();

                if (projCache == Main.instance.DrawCacheProjsBehindProjectiles)
                    PreDrawProjectiles(ref Main.spriteBatch, projectiles);

                orig(main, projCache, startSpriteBatch);

                if (projCache == Main.instance.DrawCacheProjsOverPlayers)
                    PostDrawProjectiles(ref Main.spriteBatch, projectiles);
            };
        }

        void ILoadable.Unload() { }

        private static void UpdateMatrices()
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
            PreDrawProjectiles_PixelatedAdditive(ref sb);
            PreDrawProjectiles_Additive(ref sb, projectiles);
            PreDrawProjectiles_Pixelated(ref sb);
        }

        private static void PostDrawProjectiles(ref SpriteBatch sb, IReadOnlyList<Projectile> projectiles)
        {
            PostDrawProjectiles_Pixelated(ref sb);
            PostDrawProjectiles_Additive(ref sb, projectiles);
            PostDrawProjectiles_PixelatedAdditive(ref sb);
        }

        private static void PreDrawProjectiles_Additive(ref SpriteBatch sb, IReadOnlyList<Projectile> projectiles)
        {
            var projIndex = -1;

            for (int i = 0; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IPreDrawAdditiveProjectile)
                {
                    projIndex = i;
                    break;
                }

                foreach (var _ in IPreDrawAdditiveProjectile.Hook.Enumerate(proj))
                {
                    projIndex = i;
                    break;
                }
            }

            if (projIndex < 0) return;

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
            sb.Begin(spriteBatchSpanshot);

            for (int i = projIndex; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IPreDrawAdditiveProjectile m)
                    m.PreDrawAdditive(proj);

                foreach (IPreDrawAdditiveProjectile g in IPreDrawAdditiveProjectile.Hook.Enumerate(proj))
                    g.PreDrawAdditive(proj);
            }

            sb.End();
        }

        private static void PreDrawProjectiles_PixelatedAdditive(ref SpriteBatch sb)
        {
            var rtContent = ModContent.GetInstance<PreDrawPixelatedAdditiveRenderTargetContent>();

            if (!rtContent.IsRenderedInThisFrame || !rtContent.TryGetRenderTarget(out RenderTarget2D target)) return;

            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            sb.End();
        }

        private static void PreDrawProjectiles_Pixelated(ref SpriteBatch sb)
        {
            var rtContent = ModContent.GetInstance<PreDrawPixelatedRenderTargetContent>();

            if (!rtContent.IsRenderedInThisFrame || !rtContent.TryGetRenderTarget(out RenderTarget2D target)) return;

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            sb.End();
        }

        private static void PostDrawProjectiles_Pixelated(ref SpriteBatch sb)
        {
            var rtContent = ModContent.GetInstance<PostDrawPixelatedRenderTargetContent>();

            if (!rtContent.IsRenderedInThisFrame || !rtContent.TryGetRenderTarget(out RenderTarget2D target)) return;

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            sb.End();
        }

        private static void PostDrawProjectiles_PixelatedAdditive(ref SpriteBatch sb)
        {
            var rtContent = ModContent.GetInstance<PostDrawPixelatedAdditiveRenderTargetContent>();

            if (!rtContent.IsRenderedInThisFrame || !rtContent.TryGetRenderTarget(out RenderTarget2D target)) return;

            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            sb.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            sb.End();
        }

        private static void PostDrawProjectiles_Additive(ref SpriteBatch sb, IReadOnlyList<Projectile> projectiles)
        {
            var projIndex = -1;

            for (int i = 0; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IPostDrawAdditiveProjectile)
                {
                    projIndex = i;
                    break;
                }

                foreach (var _ in IPostDrawAdditiveProjectile.Hook.Enumerate(proj))
                {
                    projIndex = i;
                    break;
                }
            }

            if (projIndex < 0) return;

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
            sb.Begin(spriteBatchSpanshot);

            for (int i = projIndex; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IPostDrawAdditiveProjectile m)
                    m.PostDrawAdditive(proj);

                foreach (IPostDrawAdditiveProjectile g in IPostDrawAdditiveProjectile.Hook.Enumerate(proj))
                    g.PostDrawAdditive(proj);
            }

            sb.End();
        }

        private static void ResetGraphicsDevice(SpriteBatchSnapshot spriteBatchSnapshot)
        {
            var device = Main.graphics.GraphicsDevice;
            device.BlendState = spriteBatchSnapshot.BlendState;
            device.SamplerStates[0] = spriteBatchSnapshot.SamplerState;
            device.DepthStencilState = spriteBatchSnapshot.DepthStencilState;
            device.RasterizerState = spriteBatchSnapshot.RasterizerState;
        }

        private abstract class PixelatedRenderTargetContent : EntityRenderTargetContent<Projectile>
        {
            public sealed override Point Size => new(Main.screenWidth / 2, Main.screenHeight / 2);
        }

        private class PreDrawPixelatedRenderTargetContent : PixelatedRenderTargetContent
        {
            public override bool CanDrawEntity(Projectile proj)
            {
                if (proj.ModProjectile is IPreDrawPixelatedProjectile)
                    return true;

                foreach (IPreDrawPixelatedProjectile _ in IPreDrawPixelatedProjectile.Hook.Enumerate(proj))
                    return true;

                return false;
            }

            public override void DrawEntity(Projectile proj)
            {
                if (proj.ModProjectile is IPreDrawPixelatedProjectile m)
                    m.PreDrawPixelated(proj);

                foreach (IPreDrawPixelatedProjectile g in IPreDrawPixelatedProjectile.Hook.Enumerate(proj))
                    g.PreDrawPixelated(proj);
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

                ResetGraphicsDevice(spriteBatchSpanshot);
                Main.spriteBatch.Begin(spriteBatchSpanshot);
                DrawEntities();
                Main.spriteBatch.End();
            }
        }

        private class PostDrawPixelatedRenderTargetContent : PixelatedRenderTargetContent
        {
            public override bool CanDrawEntity(Projectile proj)
            {
                if (proj.ModProjectile is IPostDrawPixelatedProjectile)
                    return true;

                foreach (IPostDrawPixelatedProjectile _ in IPostDrawPixelatedProjectile.Hook.Enumerate(proj))
                    return true;

                return false;
            }

            public override void DrawEntity(Projectile proj)
            {
                if (proj.ModProjectile is IPostDrawPixelatedProjectile m)
                    m.PostDrawPixelated(proj);

                foreach (IPostDrawPixelatedProjectile g in IPostDrawPixelatedProjectile.Hook.Enumerate(proj))
                    g.PostDrawPixelated(proj);
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

                ResetGraphicsDevice(spriteBatchSpanshot);
                Main.spriteBatch.Begin(spriteBatchSpanshot);
                DrawEntities();
                Main.spriteBatch.End();
            }
        }

        private class PreDrawPixelatedAdditiveRenderTargetContent : PixelatedRenderTargetContent
        {
            public override bool CanDrawEntity(Projectile proj)
            {
                if (proj.ModProjectile is IPreDrawPixelatedAdditiveProjectile)
                    return true;

                foreach (IPreDrawPixelatedAdditiveProjectile _ in IPreDrawPixelatedAdditiveProjectile.Hook.Enumerate(proj))
                    return true;

                return false;
            }

            public override void DrawEntity(Projectile proj)
            {
                if (proj.ModProjectile is IPreDrawPixelatedAdditiveProjectile m)
                    m.PreDrawPixelatedAdditive(proj);

                foreach (IPreDrawPixelatedAdditiveProjectile g in IPreDrawPixelatedAdditiveProjectile.Hook.Enumerate(proj))
                    g.PreDrawPixelatedAdditive(proj);
            }

            public override void DrawToTarget()
            {
                var spriteBatchSpanshot = new SpriteBatchSnapshot
                {
                    SortMode = SpriteSortMode.Deferred,
                    BlendState = BlendState.Additive,
                    SamplerState = Main.DefaultSamplerState,
                    DepthStencilState = DepthStencilState.None,
                    RasterizerState = Main.Rasterizer,
                    Effect = null,
                    Matrix = Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix
                };

                ResetGraphicsDevice(spriteBatchSpanshot);
                Main.spriteBatch.Begin(spriteBatchSpanshot);
                DrawEntities();
                Main.spriteBatch.End();
            }
        }

        private class PostDrawPixelatedAdditiveRenderTargetContent : PixelatedRenderTargetContent
        {
            public override bool CanDrawEntity(Projectile proj)
            {
                if (proj.ModProjectile is IPostDrawPixelatedAdditiveProjectile)
                    return true;

                foreach (IPostDrawPixelatedAdditiveProjectile _ in IPostDrawPixelatedAdditiveProjectile.Hook.Enumerate(proj))
                    return true;

                return false;
            }

            public override void DrawEntity(Projectile proj)
            {
                if (proj.ModProjectile is IPostDrawPixelatedAdditiveProjectile m)
                    m.PostDrawPixelatedAdditive(proj);

                foreach (IPostDrawPixelatedAdditiveProjectile g in IPostDrawPixelatedAdditiveProjectile.Hook.Enumerate(proj))
                    g.PostDrawPixelatedAdditive(proj);
            }

            public override void DrawToTarget()
            {
                var spriteBatchSpanshot = new SpriteBatchSnapshot
                {
                    SortMode = SpriteSortMode.Deferred,
                    BlendState = BlendState.Additive,
                    SamplerState = Main.DefaultSamplerState,
                    DepthStencilState = DepthStencilState.None,
                    RasterizerState = Main.Rasterizer,
                    Effect = null,
                    Matrix = Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix
                };

                ResetGraphicsDevice(spriteBatchSpanshot);
                Main.spriteBatch.Begin(spriteBatchSpanshot);
                DrawEntities();
                Main.spriteBatch.End();
            }
        }
    }
}