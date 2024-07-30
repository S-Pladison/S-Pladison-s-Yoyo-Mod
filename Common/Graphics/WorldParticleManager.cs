using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Threading;
using SPYoyoMod.Common.Graphics.DrawLayers;
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

        private static void UpdateParticles()
        {
            if (ParticleCount <= 0)
                return;

            var shouldBeRemovedParticles = new List<int>(ParticleCount / 4);

            FastParallel.For(0, ParticleCount, (from, to, context) =>
            {
                for (int i = from; i < to; i++)
                {
                    ref var particle = ref _particles.Buffer[i];

                    if (particle.ShouldBeRemoved)
                    {
                        _mutex.WaitOne();

                        shouldBeRemovedParticles.Add(i);

                        _mutex.ReleaseMutex();
                    }
                    else
                    {
                        particle.Update();
                    }
                }
            });

            foreach (var i in shouldBeRemovedParticles)
                _particles.RemoveAt(i);
        }

        void ILoadable.Load(Mod mod)
        {
            ModEvents.OnPreUpdateDusts += UpdateParticles;
        }

        void ILoadable.Unload()
        {
            ModEvents.OnPreUpdateDusts -= UpdateParticles;

            _particles = null;
            _mutex = null;
        }

        private sealed class WorldParticleDrawLayer : GameDrawLayer
        {
            private readonly FastList<IWorldParticle> _defaultParticles = new(MaxParticles);

            public override void Unload()
                => _defaultParticles.Clear();

            public override Position GetDefaultPosition()
                => new BeforeParent(VanillaDrawLayers.DrawDust);

            public override bool GetDefaultVisibility()
                => true;

            protected override void Draw()
            {
                if (_defaultParticles.Length > 0)
                    _defaultParticles.Clear();

                for (var i = 0; i < ParticleCount; i++)
                {
                    ref var particle = ref _particles.Buffer[i];

                    if (particle.IsPixelated)
                        continue;

                    _defaultParticles.Buffer[_defaultParticles.Length] = particle;
                    _defaultParticles.Length++;
                }

                if (_defaultParticles.Length <= 0)
                    return;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, GameMatrices.Transform);

                for (var i = 0; i < _defaultParticles.Length; i++)
                {
                    _defaultParticles.Buffer[i].Draw(Main.screenPosition);
                }

                Main.spriteBatch.End();
            }
        }

        // TODO: private sealed class WorldParticlePixelatedDrawLayer : GameDrawLayer
    }
}