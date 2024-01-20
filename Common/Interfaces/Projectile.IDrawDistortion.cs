using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [Autoload(Side = ModSide.Client)]
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
            private readonly List<Tuple<IDrawDistortionProjectile, Projectile>> instances;

            public DrawDistortionRenderTargetContent()
            {
                instances = new List<Tuple<IDrawDistortionProjectile, Projectile>>();
            }

            public override bool Active
            {
                get
                {
                    instances.Clear();

                    if (!Filters.Scene[DrawDistortionFilterSystem.FilterName].IsActive())
                        return false;

                    foreach (var proj in DrawUtils.GetActiveForDrawProjectiles())
                    {
                        if (proj.ModProjectile is IDrawDistortionProjectile m)
                            instances.Add(new(m, proj));

                        (proj.ModProjectile as IDrawDistortionProjectile)?.DrawDistortion(proj);

                        foreach (IDrawDistortionProjectile g in Hook.Enumerate(proj))
                            instances.Add(new(g, proj));
                    }

                    return instances.Count > 0;
                }
            }

            public override void DrawToTarget()
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                foreach (var (instance, proj) in instances)
                {
                    instance.DrawDistortion(proj);
                }

                Main.spriteBatch.End();

                Main.graphics.GraphicsDevice.SetRenderTarget(null);

                Filters.Scene[DrawDistortionFilterSystem.FilterName].GetShader().UseImage(renderTarget);
            }
        }
    }
}