using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.RenderTargets;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    public interface IDrawDistortionProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IDrawDistortionProjectile).GetMethod(nameof(DrawDistortion)))
            );

        void DrawDistortion(Projectile proj);

        private class DistortionRenderTargetContent : EntityRenderTargetContent<Projectile>
        {
            public const string EffectName = "ScreenDistortion";
            public const string FilterName = $"{nameof(SPYoyoMod)}:{EffectName}";

            private Texture2D inactiveTexture;

            public override void Load()
            {
                Main.QueueMainThreadAction(() =>
                {
                    inactiveTexture = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                    inactiveTexture.SetData(new Color[] { new Color(128, 128, 255) });
                });

                var effect = ModContent.Request<Effect>(ModAssets.EffectsPath + EffectName, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                var refEffect = new Ref<Effect>(effect);
                var screenShaderData = new ScreenShaderData(refEffect, EffectName + "Pass");

                Filters.Scene[FilterName] = new Filter(screenShaderData, EffectPriority.VeryHigh);
            }

            public override bool CanRender()
            {
                UpdateFilter(null);
                return true;
            }

            public override bool CanDrawEntity(Projectile proj)
            {
                if (proj.ModProjectile is IDrawDistortionProjectile m)
                    return true;

                foreach (IDrawDistortionProjectile g in Hook.Enumerate(proj))
                    return true;

                return false;
            }

            public override void DrawEntity(Projectile proj)
            {
                if (proj.ModProjectile is IDrawDistortionProjectile m)
                    m.DrawDistortion(proj);

                foreach (IDrawDistortionProjectile g in Hook.Enumerate(proj))
                    g.DrawDistortion(proj);
            }

            public override void DrawToTarget()
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                DrawEntities();
                Main.spriteBatch.End();

                Main.graphics.GraphicsDevice.SetRenderTarget(null);
                UpdateFilter(renderTarget);
            }

            public void UpdateFilter(RenderTarget2D target)
            {
                if (target is null)
                {
                    if (Filters.Scene[FilterName].IsActive())
                    {
                        Filters.Scene[FilterName].GetShader().UseImage(inactiveTexture);
                        Filters.Scene.Deactivate(FilterName);
                    }
                    return;
                }

                if (!Filters.Scene[FilterName].IsActive())
                    Filters.Scene.Activate(FilterName);

                Filters.Scene[FilterName].GetShader().UseImage(renderTarget);
            }
        }
    }
}