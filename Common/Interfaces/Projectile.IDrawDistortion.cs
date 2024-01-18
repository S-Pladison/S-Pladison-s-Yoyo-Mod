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

        public static void Draw()
        {
            foreach (var proj in Main.projectile)
            {
                if (!proj.active) continue;

                (proj.ModProjectile as IDrawDistortionProjectile)?.DrawDistortion(proj);

                foreach (IDrawDistortionProjectile g in Hook.Enumerate(proj))
                {
                    g.DrawDistortion(proj);
                }
            }
        }

        private class DrawDistortionFilterSystem : ModSystem
        {
            public const string EffectName = "ScreenDistortion";
            public const string FilterName = $"{nameof(SPYoyoMod)}:{EffectName}";

            public override void Load()
            {
                var effect = ModContent.Request<Effect>(ModAssets.EffectsPath + EffectName, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                var refEffect = new Ref<Effect>(effect);
                var screenShaderData = new ScreenShaderData(refEffect, EffectName + "Pass");

                Filters.Scene[FilterName] = new Filter(screenShaderData, EffectPriority.VeryHigh);
            }

            public override void OnWorldLoad()
            {
                Filters.Scene.Activate(FilterName);
            }

            public override void OnWorldUnload()
            {
                Filters.Scene.Deactivate(FilterName);
            }
        }

        private class DrawDistortionRenderTargetContent : ScreenRenderTargetContent
        {
            public override bool Active { get => Filters.Scene[DrawDistortionFilterSystem.FilterName].IsActive(); }

            public override void DrawToTarget()
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                Draw();
                Main.spriteBatch.End();

                Main.graphics.GraphicsDevice.SetRenderTarget(null);

                Filters.Scene[DrawDistortionFilterSystem.FilterName].GetShader().UseImage(renderTarget);
            }
        }
    }
}