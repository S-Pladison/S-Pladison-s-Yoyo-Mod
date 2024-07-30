using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Threading;
using SPYoyoMod.Common.Graphics.RenderTargets;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Graphics
{
    public interface IWorldParticle
    {
        bool IsPixelated { get; }
        bool ShouldBeRemoved { get; }

        void Update();
        void Draw(Vector2 screenPosition);
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class WorldParticleManager : ILoadable
    {
        public const int MaxParticles = 1000;

        private static Mutex _mutex = new();
        private static FastList<IWorldParticle> _particles = new(MaxParticles);
        private static FastList<IWorldParticle> _tempParticles = new(MaxParticles);
        private static ScreenRenderTarget _pixelatedTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.TwiceSmaller);
        private static bool _pixelatedTargetWasPrepared;

        public static int ParticleCount
        {
            get => _particles.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SpawnParticle<T>() where T : IWorldParticle, new()
            => SpawnParticle(new T());

        public static T SpawnParticle<T>(T particle) where T : IWorldParticle
        {
            if (ParticleCount >= MaxParticles)
                return particle;

            _particles.Buffer[_particles.Length] = particle;
            _particles.Length++;

            return particle;
        }

        void ILoadable.Load(Mod mod)
        {
            ModEvents.OnPreUpdateDusts += UpdateParticles;
            ModEvents.OnPostUpdateCameraPosition += RenderPixelatedParticles;

            On_Main.DrawDust += (orig, main) =>
            {
                DrawDefaultParticles();
                DrawPixelatedParticles();

                orig(main);
            };
        }

        void ILoadable.Unload()
        {
            ModEvents.OnPreUpdateDusts -= UpdateParticles;

            _pixelatedTarget = null;
            _tempParticles = null;
            _particles = null;
            _mutex = null;
        }

        private static void UpdateParticles()
        {
            if (ParticleCount <= 0)
                return;

            var shouldBeRemovedParticles = _tempParticles;

            FastParallel.For(0, ParticleCount, (from, to, context) =>
            {
                for (int i = from; i < to; i++)
                {
                    ref var particle = ref _particles.Buffer[i];

                    if (particle.ShouldBeRemoved)
                    {
                        _mutex.WaitOne();

                        shouldBeRemovedParticles.Buffer[shouldBeRemovedParticles.Length] = particle;
                        shouldBeRemovedParticles.Length++;

                        _mutex.ReleaseMutex();
                    }
                    else
                    {
                        particle.Update();
                    }
                }
            });

            for (int i = 0; i < shouldBeRemovedParticles.Length; i++)
                _particles.RemoveAt(i);

            shouldBeRemovedParticles.Clear();
        }

        private static ref FastList<IWorldParticle> GetParticlesByCondition(Predicate<IWorldParticle> predicate)
        {
            _tempParticles.Clear();

            for (var i = 0; i < ParticleCount; i++)
            {
                ref var particle = ref _particles.Buffer[i];

                if (predicate(particle))
                {
                    _tempParticles.Buffer[_tempParticles.Length] = particle;
                    _tempParticles.Length++;
                }
            }

            return ref _tempParticles;
        }

        private static void DrawDefaultParticles()
        {
            ref var defaultParticles = ref GetParticlesByCondition(p => !p.IsPixelated);

            if (defaultParticles.Length <= 0)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, GameMatrices.Transform);

            for (var i = 0; i < defaultParticles.Length; i++)
            {
                defaultParticles.Buffer[i].Draw(Main.screenPosition);
            }

            Main.spriteBatch.End();
        }

        private static void RenderPixelatedParticles()
        {
            var device = Main.graphics.GraphicsDevice;
            ref var pixelatedParticles = ref GetParticlesByCondition(p => p.IsPixelated);

            if (pixelatedParticles.Length <= 0)
                return;

            _pixelatedTargetWasPrepared = false;

            device.SetRenderTarget(_pixelatedTarget);
            device.Clear(Color.Transparent);
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, GameMatrices.Effect * Matrix.CreateScale(0.5f));

                for (var i = 0; i < pixelatedParticles.Length; i++)
                {
                    pixelatedParticles.Buffer[i].Draw(Main.screenPosition);
                }

                Main.spriteBatch.End();
            }
            device.SetRenderTarget(null);

            _pixelatedTargetWasPrepared = true;
        }

        private static void DrawPixelatedParticles()
        {
            if (!_pixelatedTargetWasPrepared)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, GameMatrices.Zoom);
            Main.spriteBatch.Draw(_pixelatedTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            Main.spriteBatch.End();

            _pixelatedTargetWasPrepared = false;
        }
    }
}