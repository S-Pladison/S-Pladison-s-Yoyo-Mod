using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Core.Graphics.RenderTargets;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Core.Graphics
{
    /// <summary>
    /// Флаги отрисовки мировой частицы.
    /// </summary>
    [Flags]
    public enum WorldParticleFlags
    {
        /// <summary>
        /// Без каких либо свойств. Частица будет отрисована перед отрисовкой ванильной пыли.
        /// </summary>
        None = 0,

        /// <summary>
        /// Частица будет отрисовываться с эффектом пикселизации. Подобные частицы рисуются перед отрисовкой обычных частиц.
        /// </summary>
        Pixelated = 1 << 0,

        /// <summary>
        /// Частица будет отрисовываться позади сущностей.
        /// </summary>
        Behind = 1 << 1
    }

    /// <summary>
    /// Интерфейс, описывающий базововую структуру мировой частицы.
    /// </summary>
    public interface IWorldParticle
    {
        /// <summary>
        /// Должена ли частица быть удалена в текущем цикле обновления?
        /// </summary>
        bool ShouldBeRemoved { get; }

        /// <summary>
        /// Метод обновления частицы.
        /// </summary>
        void Update();

        /// <summary>
        /// Метод отрисовки частицы.
        /// </summary>
        void Draw(SpriteBatch spriteBatch, in Vector2 screenPosition);
    }

    /// <summary>
    /// Менеджер, отвечающий за работу с мировыми частицами.
    /// Суть такая же, что и у ванильной пыли, но в отличии от них, позволяет рисовать с определенными эффектами на определенных 'слоях' игры.
    /// <br/>Для добавления новой частицы используй функцию <see cref="SpawnParticle{T}(WorldParticleFlags)"/>.
    /// </summary>
    public sealed class WorldParticleManager : ILoadable
    {
        /// <summary>
        /// Создает частицу в игровом мире. Если в мультиплеере на стороне сервера попытаться вызвать эту функцию, то объект частицы будет создан,
        /// но в список активных частиц она добавлена не будет.
        /// </summary>
        public static T SpawnParticle<T>(WorldParticleFlags flags = WorldParticleFlags.None) where T : IWorldParticle, new()
        {
            var particle = new T();

            if (Main.dedServ)
                return particle;

            _particles[flags].Add(particle);
            return particle;
        }

        private sealed class PixelatedLayer(IReadOnlyList<IWorldParticle> particles)
        {
            private readonly ScreenRenderTarget _renderTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.TwiceSmaller);
            private readonly IReadOnlyList<IWorldParticle> _particles = particles;
            private bool _targetWasPrepared = false;

            public void Render()
            {
                if (_particles.Count == 0)
                    return;

                _targetWasPrepared = false;

                var device = Main.graphics.GraphicsDevice;
                var spriteBatchSpanshot = new SpriteBatchSnapshot
                {
                    SortMode = SpriteSortMode.Deferred,
                    BlendState = BlendState.AlphaBlend,
                    SamplerState = Main.DefaultSamplerState,
                    DepthStencilState = DepthStencilState.None,
                    RasterizerState = Main.Rasterizer,
                    Effect = null,
                    Matrix = GameMatrices.Effect * Matrix.CreateScale(0.5f)
                };

                device.SetRenderTarget(_renderTarget);
                device.Clear(Color.Transparent);
                {
                    Main.spriteBatch.Begin(spriteBatchSpanshot);
                    foreach (var particle in _particles)
                    {
                        particle.Draw(Main.spriteBatch, Main.screenPosition);
                    }
                    Main.spriteBatch.End();
                }
                device.SetRenderTarget(null);

                _targetWasPrepared = true;
            }

            public void Draw()
            {
                if (!_targetWasPrepared)
                    return;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, GameMatrices.Zoom);
                Main.spriteBatch.Draw(_renderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                Main.spriteBatch.End();

                _targetWasPrepared = false;
            }
        }

        private static readonly Dictionary<WorldParticleFlags, List<IWorldParticle>> _particles = [];
        private static readonly Dictionary<WorldParticleFlags, PixelatedLayer> _pixelatedLayers = [];
        private static List<WorldParticleFlags> _flagCombinations = [];

        void ILoadable.Load(Mod mod)
        {
            if (Main.dedServ)
                return;

            foreach (var flags in _flagCombinations = EnumUtils.GetVariants<WorldParticleFlags>().ToList())
            {
                _particles[flags] = [];

                if (!flags.HasFlag(WorldParticleFlags.Pixelated))
                    continue;

                _pixelatedLayers[flags] = new(_particles[flags].AsReadOnly());
            }

            ModEvents.OnWorldUnload += ClearParticles;
            ModEvents.OnPreUpdateDusts += UpdateParticles;
            ModEvents.OnPostUpdateCameraPosition += RenderPixelatedLayers;

            On_Main.DoDraw_DrawNPCsOverTiles += (orig, main) =>
            {
                DrawSpecificParticles(WorldParticleFlags.Pixelated | WorldParticleFlags.Behind);
                DrawSpecificParticles(WorldParticleFlags.Behind);

                orig(main);
            };

            On_Main.DrawDust += (orig, main) =>
            {
                DrawSpecificParticles(WorldParticleFlags.Pixelated);
                DrawSpecificParticles(WorldParticleFlags.None);

                orig(main);
            };
        }

        void ILoadable.Unload()
        {
            if (Main.dedServ)
                return;

            ModEvents.OnPostUpdateCameraPosition -= RenderPixelatedLayers;
            ModEvents.OnPreUpdateDusts -= UpdateParticles;
            ModEvents.OnWorldUnload -= ClearParticles;

            ClearParticles();

            _flagCombinations.Clear();
            _pixelatedLayers.Clear();
            _particles.Clear();
        }

        private static void ClearParticles()
        {
            foreach (var flags in _flagCombinations)
            {
                _particles[flags].Clear();
            }
        }

        private static void UpdateParticles()
        {
            foreach (var flags in _flagCombinations)
            {
                _particles[flags].ForEach(p => p.Update());
                _particles[flags].RemoveAll(p => p.ShouldBeRemoved);
            }
        }

        private static void RenderPixelatedLayers()
        {
            foreach (var (_, layer) in _pixelatedLayers)
            {
                layer.Render();
            }
        }

        private static void DrawSpecificParticles(WorldParticleFlags flags)
        {
            if (_particles[flags].Count == 0)
                return;

            if (flags.HasFlag(WorldParticleFlags.Pixelated))
            {
                _pixelatedLayers[flags].Draw();
                return;
            }

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, GameMatrices.Transform);
            foreach (var particle in _particles[flags])
            {
                particle.Draw(Main.spriteBatch, Main.screenPosition);
            }
            Main.spriteBatch.End();
        }
    }
}