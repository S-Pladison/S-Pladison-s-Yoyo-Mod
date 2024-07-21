using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Graphics.RenderTargets
{
    public enum ScreenRenderTargetScale
    {
        Default,
        TwiceSmaller
    }

    /// <summary>
    /// Класс-оболочка для <see cref="RenderTarget2D"/>, который безопасно обрабатывает изменение размера, выгрузку
    /// и автоматическое удаление, если он в данный момент не используется для экономии памяти графического процессора.<br/>
    /// В отличии от <see cref="ManagedRenderTarget"/>, размер устанавливается не вручную, а автоматически в зависимости
    /// от разрешения экрана.
    /// </summary>
    public sealed class ScreenRenderTarget
    {
        /// <summary>
        /// Создает объект управляемой цели рендеринга, размер которого зависит от разрешения экрана игры.
        /// </summary>
        public static ScreenRenderTarget Create(ScreenRenderTargetScale scale, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
        {
            var target = new ScreenRenderTarget(
                ManagedRenderTarget.Create(Main.screenWidth, Main.screenHeight, mipMap, preferredFormat, preferredDepthFormat, preferredMultiSampleCount, usage),
                scale
            );

            ScreenRenderTargetSystem.ScreenTargets.Add(target);

            return target;
        }

        /// <inheritdoc cref="Create"/>
        public static ScreenRenderTarget Create(ScreenRenderTargetScale scale, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat)
            => Create(scale, mipMap, preferredFormat, preferredDepthFormat, 0, RenderTargetUsage.DiscardContents);

        /// <inheritdoc cref="Create"/>
        public static ScreenRenderTarget Create(ScreenRenderTargetScale scale)
            => Create(scale, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

        private readonly ManagedRenderTarget _managedTarget;
        private readonly ScreenRenderTargetScale _scale;

        private ScreenRenderTarget(ManagedRenderTarget managedTarget, ScreenRenderTargetScale scale)
        {
            _managedTarget = managedTarget;
            _scale = scale;
        }

        public int Width => _managedTarget.Width;
        public int Height => _managedTarget.Height;
        public Vector2 Size => _managedTarget.Size;
        public bool IsUninitialized => _managedTarget.IsUninitialized;
        public bool IsDisposed => _managedTarget.IsDisposed;
        public bool WaitingForFirstInitialization => _managedTarget.WaitingForFirstInitialization;
        public RenderTarget2D Target => _managedTarget.Target;

        public static implicit operator RenderTarget2D(ScreenRenderTarget target)
            => target.Target;

        [Autoload(Side = ModSide.Client)]
        [LoadPriority(sbyte.MaxValue)]
        private class ScreenRenderTargetSystem : ModSystem
        {
            public static List<ScreenRenderTarget> ScreenTargets = [];

            public override void OnModLoad()
            {
                ModEvents.OnResolutionChanged += ResizeTargets;
            }

            public override void OnModUnload()
            {
                ModEvents.OnResolutionChanged -= ResizeTargets;

                Main.QueueMainThreadAction(() =>
                {
                    foreach (var screenTarget in ScreenTargets)
                        screenTarget._managedTarget?.Dispose();

                    ScreenTargets.Clear();
                });
            }

            public override void PostSetupContent()
            {
                // Да, костыльно немного... но на всякий случай... пусть будет
                ResizeTargets(Main.ScreenSize);
            }

            private static void ResizeTargets(Point screenSize)
            {
                foreach (var screenTarget in ScreenTargets)
                {
                    var size = GetTargetSize(screenSize, screenTarget._scale);
                    screenTarget._managedTarget.Resize(size.X, size.Y);
                }
            }

            private static Point GetTargetSize(Point screenSize, ScreenRenderTargetScale scale)
            {
                return scale switch
                {
                    ScreenRenderTargetScale.TwiceSmaller => new(screenSize.X / 2, screenSize.Y / 2),
                    _ => screenSize,
                };
            }
        }
    }
}
