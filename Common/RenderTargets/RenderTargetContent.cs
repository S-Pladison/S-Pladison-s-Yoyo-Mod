using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.RenderTargets
{
    [Autoload(Side = ModSide.Client)]
    public abstract class RenderTargetContent : ModType
    {
        public abstract bool Active { get; }
        public abstract Point Size { get; }
        public virtual Color ClearColor { get => Color.Transparent; }

        protected RenderTarget2D renderTarget;

        private bool wasRendered;

        public abstract void DrawToTarget();

        public void Render(GraphicsDevice device)
        {
            PrepareRenderTarget(device);

            device.SetRenderTarget(renderTarget);
            device.Clear(ClearColor);

            DrawToTarget();

            wasRendered = true;
        }

        public bool TryGetRenderTarget(out RenderTarget2D renderTarget)
        {
            if (!wasRendered)
            {
                renderTarget = null;
                return false;
            }

            renderTarget = this.renderTarget;
            return true;
        }

        protected sealed override void Register()
        {
            ModTypeLookup<RenderTargetContent>.Register(this);
            RenderTargetContentSystem.Register(this);
        }

        protected void PrepareRenderTarget(GraphicsDevice device)
        {
            if (renderTarget is not null
                && !renderTarget.IsDisposed
                && renderTarget.Width == Size.X
                && renderTarget.Height == Size.Y) return;

            wasRendered = false;
            renderTarget = new(device, Size.X, Size.Y, false, device.PresentationParameters.BackBufferFormat, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
        }
    }
}