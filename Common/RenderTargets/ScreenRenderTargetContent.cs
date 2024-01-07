using Microsoft.Xna.Framework;
using Terraria;

namespace SPYoyoMod.Common.RenderTargets
{
    public abstract class ScreenRenderTargetContent : RenderTargetContent
    {
        public sealed override Point Size { get => new(Main.screenWidth, Main.screenHeight); }
    }
}