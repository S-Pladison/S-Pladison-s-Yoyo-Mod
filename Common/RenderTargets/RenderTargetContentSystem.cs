using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.RenderTargets
{
    [Autoload(Side = ModSide.Client)]
    public sealed class RenderTargetContentSystem : ModSystem
    {
        private static List<RenderTargetContent> contentThatNeedsRenderTargets;

        static RenderTargetContentSystem()
        {
            contentThatNeedsRenderTargets = new();
        }

        public override void Load()
        {
            On_Main.DoDraw_UpdateCameraPosition += (orig) =>
            {
                orig();

                var device = Main.graphics.GraphicsDevice;
                var targets = device.GetRenderTargets();

                foreach (var screenTarget in contentThatNeedsRenderTargets)
                {
                    if (!screenTarget.Active) continue;

                    screenTarget.Render(device);
                }

                device.SetRenderTargets(targets);
            };
        }

        public override void Unload()
        {
            contentThatNeedsRenderTargets.Clear();
            contentThatNeedsRenderTargets = null;
        }

        internal static void Register(RenderTargetContent screenTarget)
        {
            contentThatNeedsRenderTargets.Add(screenTarget);
        }
    }
}