using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.RenderTargets
{
    [Autoload(Side = ModSide.Client)]
    public abstract class RenderTargetContent : ModType, IDisposable
    {
        public bool IsRenderedInThisFrame { get; private set; }
        public abstract Point Size { get; }
        public virtual Color ClearColor { get => Color.Transparent; }

        protected RenderTarget2D renderTarget;

        private bool wasRendered;

        public abstract void DrawToTarget();

        public virtual bool PreRender() { return true; }

        public void Reset()
        {
            IsRenderedInThisFrame = false;
        }

        public void Render(GraphicsDevice device)
        {
            PrepareRenderTarget(device);

            device.SetRenderTarget(renderTarget);
            device.Clear(ClearColor);

            DrawToTarget();

            wasRendered = true;

            IsRenderedInThisFrame = true;
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

        public sealed override void SetupContent()
        {
            SetStaticDefaults();
        }

        public void Dispose()
        {
            renderTarget?.Dispose();
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

            renderTarget?.Dispose();
            renderTarget = new(device, Size.X, Size.Y, false, device.PresentationParameters.BackBufferFormat, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
        }
    }
}