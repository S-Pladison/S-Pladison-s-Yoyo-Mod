using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
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

        private class DrawDistortionRenderTargetContent : ScreenRenderTargetContent
        {
            public const string EffectName = "ScreenDistortion";
            public const string FilterName = $"{nameof(SPYoyoMod)}:{EffectName}";

            private List<Tuple<IDrawDistortionProjectile, Projectile>> instances;
            private Texture2D inactiveTexture;

            public override bool Active
            {
                get
                {
                    instances.Clear();

                    foreach (var proj in DrawUtils.GetActiveForDrawProjectiles())
                    {
                        if (proj.ModProjectile is IDrawDistortionProjectile m)
                            instances.Add(new(m, proj));

                        foreach (IDrawDistortionProjectile g in Hook.Enumerate(proj))
                            instances.Add(new(g, proj));
                    }

                    var isFilterActive = Filters.Scene[FilterName].IsActive();

                    if (instances.Count > 0)
                    {
                        if (!isFilterActive)
                            Filters.Scene.Activate(FilterName);

                        return true;
                    }

                    if (isFilterActive)
                    {
                        Filters.Scene[FilterName].GetShader().UseImage(inactiveTexture);
                        Filters.Scene.Deactivate(FilterName);
                    }

                    return false;
                }
            }

            public override void Load()
            {
                instances = new List<Tuple<IDrawDistortionProjectile, Projectile>>();

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

            public override void DrawToTarget()
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                foreach (var (instance, proj) in instances)
                {
                    instance.DrawDistortion(proj);
                }

                Main.spriteBatch.End();

                Main.graphics.GraphicsDevice.SetRenderTarget(null);

                Filters.Scene[FilterName].GetShader().UseImage(renderTarget);
            }
        }
    }
}