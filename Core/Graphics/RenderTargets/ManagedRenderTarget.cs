using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Core.Graphics.RenderTargets
{
    // [ Based on code from the Calamity Mod GitHub Repository (https://github.com/CalamityTeam/CalamityModPublic/) ]

    /// <summary>
    /// Класс-оболочка для <see cref="RenderTarget2D"/>, который безопасно обрабатывает изменение размера, выгрузку
    /// и автоматическое удаление, если он в данный момент не используется для экономии памяти графического процессора.
    /// </summary>
    public sealed class ManagedRenderTarget : IDisposable
    {
        /// <summary>
        /// Создает объект управляемой цели рендеринга. Удивительно...
        /// </summary>
        public static ManagedRenderTarget Create(int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
        {
            if (Main.dedServ)
                return null;

            var info = new RenderTargetInfo(width, height, mipMap, preferredFormat, preferredDepthFormat, preferredMultiSampleCount, usage);
            var target = new ManagedRenderTarget(info);

            ManagedRenderTargetSystem.RegisterTarget(target);

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

        public int Width => _info.Width;
        public int Height => _info.Height;
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
        private struct RenderTargetInfo(int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
        {
            public int Width = width;
            public int Height = height;
            public bool MipMap = mipMap;
            public SurfaceFormat PreferredFormat = preferredFormat;
            public DepthFormat PreferredDepthFormat = preferredDepthFormat;
            public int PreferredMultiSampleCount = preferredMultiSampleCount;
            public RenderTargetUsage Usage = usage;
        };

        [Autoload(Side = ModSide.Client)]
        private sealed class ManagedRenderTargetSystem : ModSystem
        {
            public static readonly int TimeBeforeAutoDispose = ModUtils.SecondsToTicks(60);

            private static readonly List<ManagedRenderTarget> _managedTargets = [];
            private static readonly Mutex _mutex = new();

            public override void OnModLoad()
            {
                ModEvents.OnPreDraw += HandleTargets;
            }

            public override void OnModUnload()
            {
                ModEvents.OnPreDraw -= HandleTargets;

                Main.QueueMainThreadAction(() =>
                {
                    _mutex.WaitOne();

                    foreach (var managedTarget in _managedTargets)
                        managedTarget?.Dispose();

                    _managedTargets.Clear();

                    _mutex.ReleaseMutex();
                });
            }

            public static void RegisterTarget(ManagedRenderTarget target)
            {
                _mutex.WaitOne();
                _managedTargets.Add(target);
                _mutex.ReleaseMutex();
            }

            private static void HandleTargets()
            {
                _mutex.WaitOne();

                foreach (var managedTarget in _managedTargets)
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

                _mutex.ReleaseMutex();
            }
        }
    }
}