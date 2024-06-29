using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using static SPYoyoMod.Utils.ModUtils;

namespace SPYoyoMod.Common.Graphics
{
    // [ Based on code from the Calamity Mod GitHub Repository (https://github.com/CalamityTeam/CalamityModPublic/) ]

    /// <summary>
    /// Класс-оболочка для <see cref="RenderTarget2D"/>, который безопасно обрабатывает изменение размера, выгрузку<br/>
    /// и автоматическое удаление, если он в данный момент не используется для экономии памяти графического процессора.
    /// </summary>
    public class ManagedRenderTarget : IDisposable
    {
        /// <summary>
        /// Создает объект управляемой цели рендеринга. Удивительно...
        /// </summary>
        public static ManagedRenderTarget Create(int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
        {
            var info = new RenderTargetInfo(width, height, mipMap, preferredFormat, preferredDepthFormat, preferredMultiSampleCount, usage);
            var target = new ManagedRenderTarget(info);

            RenderTargetSystem.ManagedTargets.Add(target);

            return target;
        }

        /// <inheritdoc cref="Create"/>
        public static ManagedRenderTarget Create(int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat)
            => Create(width, height, mipMap, preferredFormat, preferredDepthFormat, 0, RenderTargetUsage.DiscardContents);

        /// <inheritdoc cref="Create"/>
        public static ManagedRenderTarget Create(int width, int height)
            => Create(width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

        private RenderTarget2D _target;
        private RenderTargetInfo _info;
        private int _timeSinceLastAccessed;

        public int Width => Target.Width;
        public int Height => Target.Height;
        public Vector2 Size => new(Width, Height);
        public bool IsUninitialized => _target is null || _target.IsDisposed;

        public bool IsDisposed
        {
            get;
            private set;
        }

        public bool WaitingForFirstInitialization
        {
            get;
            private set;
        }

        public RenderTarget2D Target
        {
            get
            {
                if (IsUninitialized)
                    InitTarget();

                _timeSinceLastAccessed = 0;
                return _target;
            }
            private set => _target = value;
        }

        private ManagedRenderTarget(RenderTargetInfo info)
        {
            WaitingForFirstInitialization = true;
            _info = info;
        }

        public void Resize(int width, int height)
        {
            if (_info.Width == width && _info.Height == height)
                return;

            _info.Width = width;
            _info.Height = height;

            if (IsUninitialized)
                return;

            Dispose();
            InitTarget();
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            _target?.Dispose();
            GC.SuppressFinalize(this);
        }

        private void InitTarget()
        {
            IsDisposed = false;
            WaitingForFirstInitialization = false;

            _timeSinceLastAccessed = 0;
            _target = new(
                Main.graphics.GraphicsDevice,
                _info.Width,
                _info.Height,
                _info.MipMap,
                _info.PreferredFormat,
                _info.PreferredDepthFormat,
                _info.PreferredMultiSampleCount,
                _info.Usage
            );
        }

        public static implicit operator RenderTarget2D(ManagedRenderTarget target)
            => target.Target;

        /// <summary>
        /// Информация о цели рендеринга.
        /// </summary>
        private struct RenderTargetInfo
        {
            public int Width;
            public int Height;
            public bool MipMap;
            public SurfaceFormat PreferredFormat;
            public DepthFormat PreferredDepthFormat;
            public int PreferredMultiSampleCount;
            public RenderTargetUsage Usage;

            public RenderTargetInfo(int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
            {
                Width = width;
                Height = height;
                MipMap = mipMap;
                PreferredFormat = preferredFormat;
                PreferredDepthFormat = preferredDepthFormat;
                PreferredMultiSampleCount = preferredMultiSampleCount;
                Usage = usage;
            }
        };

        /// <summary>
        /// Система, обрабатывающая все созданные экземпляры <see cref="ManagedRenderTarget"/>.
        /// </summary>
        private class RenderTargetSystem : ModSystem
        {
            public static readonly int TimeBeforeAutoDispose = SecondsToTicks(60);
            public static List<ManagedRenderTarget> ManagedTargets = new();

            public override void OnModLoad()
            {
                ModEvents.OnPreDraw += HandleTargets;
            }

            public override void OnModUnload()
            {
                ModEvents.OnPreDraw -= HandleTargets;

                Main.QueueMainThreadAction(() =>
                {
                    foreach (var managedTarget in ManagedTargets)
                        managedTarget?.Dispose();

                    ManagedTargets.Clear();
                });
            }

            private static void HandleTargets()
            {
                foreach (var managedTarget in ManagedTargets)
                {
                    if (managedTarget.IsDisposed)
                        continue;

                    if (managedTarget._timeSinceLastAccessed >= TimeBeforeAutoDispose)
                    {
                        managedTarget.Dispose();
                        continue;
                    }

                    managedTarget._timeSinceLastAccessed++;
                }
            }
        }
    }
}