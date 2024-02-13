using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.RenderTargets
{
    [Autoload(Side = ModSide.Client)]
    public sealed class RenderTargetContentSystem : ModSystem
    {
        private static List<RenderTargetContent> contentThatNeedsRenderTargets = new();

        public override void PostSetupContent()
        {
            On_Main.DoDraw_UpdateCameraPosition += (orig) =>
            {
                orig();

                var device = Main.graphics.GraphicsDevice;
                var targets = device.GetRenderTargets();

                for (var i = 0; i < contentThatNeedsRenderTargets.Count; i++)
                {
                    var content = contentThatNeedsRenderTargets[i];
                    content.Reset();

                    if (!content.PreRender()) continue;

                    content.Render(device);
                }

                device.SetRenderTargets(targets);

                Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            };
        }

        public override void Unload()
        {
            Main.QueueMainThreadAction(() =>
            {
                contentThatNeedsRenderTargets.ForEach(x => x?.Dispose());
                contentThatNeedsRenderTargets.Clear();
                contentThatNeedsRenderTargets = null;
            });
        }

        internal static void Register(RenderTargetContent content)
        {
            contentThatNeedsRenderTargets.Add(content);
        }
    }
}