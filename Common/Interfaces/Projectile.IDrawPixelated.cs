using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.RenderTargets;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    public interface IDrawPixelatedProjectile : IPreDrawPixelatedProjectile, IPostDrawPixelatedProjectile { }

    public interface IPreDrawPixelatedProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook;

        static IPreDrawPixelatedProjectile()
        {
            Hook = ProjectileLoader.AddModHook(new GlobalHookList<GlobalProjectile>(typeof(IPreDrawPixelatedProjectile).GetMethod(nameof(PreDrawPixelated))));
        }

        void PreDrawPixelated(Projectile proj);

        #region Implementation
        private class PreDrawPixelizationRenderTargetContent : RenderTargetContent
        {
            public override bool Active { get => true; }
            public override Point Size { get => new(Main.screenWidth / 2, Main.screenHeight / 2); }

            public override void Load()
            {
                On_Main.DrawProjectiles += (orig, main) =>
                {
                    if (TryGetRenderTarget(out RenderTarget2D target))
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                        Main.spriteBatch.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                        Main.spriteBatch.End();
                    }

                    orig(main);
                };
            }

            public override void DrawToTarget()
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix);

                foreach (var proj in Main.projectile)
                {
                    if (!proj.active) continue;

                    (proj.ModProjectile as IPreDrawPixelatedProjectile)?.PreDrawPixelated(proj);

                    foreach (IPreDrawPixelatedProjectile g in Hook.Enumerate(proj))
                    {
                        g.PreDrawPixelated(proj);
                    }
                }

                Main.spriteBatch.End();
            }
        }
        #endregion
    }

    public interface IPostDrawPixelatedProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook;

        static IPostDrawPixelatedProjectile()
        {
            Hook = ProjectileLoader.AddModHook(new GlobalHookList<GlobalProjectile>(typeof(IPostDrawPixelatedProjectile).GetMethod(nameof(PostDrawPixelated))));
        }

        void PostDrawPixelated(Projectile proj);

        #region Implementation
        private class PostDrawPixelizationRenderTargetContent : RenderTargetContent
        {
            public override bool Active { get => true; }
            public override Point Size { get => new(Main.screenWidth / 2, Main.screenHeight / 2); }

            public override void Load()
            {
                On_Main.DrawCachedProjs += (orig, main, projCache, startSpriteBatch) =>
                {
                    orig(main, projCache, startSpriteBatch);

                    if (projCache != Main.instance.DrawCacheProjsOverPlayers || !TryGetRenderTarget(out RenderTarget2D target)) return;

                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                    Main.spriteBatch.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                };
            }

            public override void DrawToTarget()
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix);

                foreach (var proj in Main.projectile)
                {
                    if (!proj.active) continue;

                    (proj.ModProjectile as IPostDrawPixelatedProjectile)?.PostDrawPixelated(proj);

                    foreach (IPostDrawPixelatedProjectile g in Hook.Enumerate(proj))
                    {
                        g.PostDrawPixelated(proj);
                    }
                }

                Main.spriteBatch.End();
            }
        }
        #endregion
    }
}