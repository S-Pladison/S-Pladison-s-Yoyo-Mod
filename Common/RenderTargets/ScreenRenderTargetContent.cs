using Microsoft.Xna.Framework;
using Terraria;

namespace SPYoyoMod.Common.RenderTargets
{
    public abstract class ScreenRenderTargetContent : RenderTargetContent
    {
        public override Point Size { get => new(Main.screenWidth, Main.screenHeight); }
    }
}