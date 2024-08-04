using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Threading;
using SPYoyoMod.Common.Graphics.RenderTargets;
using SPYoyoMod.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private static List<IWorldParticle> _particles = new(MaxParticles);
        private static ScreenRenderTarget _pixelatedTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.TwiceSmaller);
        private static bool _pixelatedTargetWasPrepared;

        public static int ParticleCount { get => _particles.Count; }

        public static T SpawnParticle<T>() where T : IWorldParticle, new()
            => SpawnParticle(new T());

        public static T SpawnParticle<T>(T particle) where T : IWorldParticle
        {
            if (Main.dedServ)
                return particle;

            if (_particles.Count >= MaxParticles)
                return particle;

            _particles.Add(particle);

            return particle;
        }

        void ILoadable.Load(Mod mod)
        {
            ModEvents.OnPreUpdateDusts += UpdateParticles;
            ModEvents.OnPostUpdateCameraPosition += RenderPixelatedParticles;
            On_Main.DrawDust += DrawDustLayer;
        }

        void ILoadable.Unload()
        {
            ModEvents.OnPreUpdateDusts -= UpdateParticles;

            _pixelatedTarget = null;
            _particles = null;
            _mutex = null;
        }

        private static void UpdateParticles()
        {
            if (_particles.Count <= 0)
                return;

            var shouldBeRemovedParticles = new List<IWorldParticle>(MaxParticles / 10);

            FastParallel.For(0, _particles.Count, (from, to, context) =>
            {
                for (int i = from; i < to; i++)
                {
                    var particle = _particles[i];

                    if (particle.ShouldBeRemoved)
                    {
                        _mutex.WaitOne();

                        shouldBeRemovedParticles.Add(particle);

                        _mutex.ReleaseMutex();
                    }
                    else
                    {
                        particle.Update();
                    }
                }
            });

            shouldBeRemovedParticles.ForEach(p => _particles.Remove(p));
        }

        private static void DrawDustLayer(On_Main.orig_DrawDust orig, Main main)
        {
            if (_particles.Count > 0)
            {
                DrawDefaultParticles();
                DrawPixelatedParticles();
            }

            orig(main);
        }

        private static void DrawDefaultParticles()
        {
            var defaultParticles = _particles.Where(p => !p.IsPixelated);

            if (!defaultParticles.Any())
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, GameMatrices.Transform);

            foreach (var particle in defaultParticles)
                particle.Draw(Main.screenPosition);

            Main.spriteBatch.End();
        }

        private static void RenderPixelatedParticles()
        {
            if (_particles.Count <= 0)
                return;

            var device = Main.graphics.GraphicsDevice;
            var pixelatedParticles = _particles.Where(p => p.IsPixelated);

            if (!pixelatedParticles.Any())
                return;

            _pixelatedTargetWasPrepared = false;

            device.SetRenderTarget(_pixelatedTarget);
            device.Clear(Color.Transparent);
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, GameMatrices.Effect * Matrix.CreateScale(0.5f));

                foreach (var particle in pixelatedParticles)
                    particle.Draw(Main.screenPosition);

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