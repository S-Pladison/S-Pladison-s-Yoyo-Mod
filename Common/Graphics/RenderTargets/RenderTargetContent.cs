﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Graphics.RenderTargets
{
    [Autoload(Side = ModSide.Client)]
    public abstract class RenderTargetContent : ModType, IDisposable
    {
        public abstract Point Size { get; }
        public virtual Color ClearColor { get => Color.Transparent; }
        public virtual uint? AutoDisposeTime { get => 60 * 60 * 3; }
        public bool WasRenderedInThisFrame { get; private set; }

        protected RenderTarget2D renderTarget;

        private uint autoDisposeTimer;
        private bool wasRendered;

        public abstract void DrawToTarget();

        public virtual bool PreRender() { return true; }

        public void Update()
        {
            WasRenderedInThisFrame = false;

            if (AutoDisposeTime is null || renderTarget is null || renderTarget.IsDisposed)
                return;

            if (autoDisposeTimer >= AutoDisposeTime)
            {
                renderTarget?.Dispose();
                wasRendered = false;
                autoDisposeTimer = 0;
                return;
            }

            autoDisposeTimer++;
        }

        public void Render(GraphicsDevice device)
        {
            PrepareRenderTarget(device);

            device.SetRenderTarget(renderTarget);
            device.Clear(ClearColor);

            DrawToTarget();

            wasRendered = true;

            WasRenderedInThisFrame = true;
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