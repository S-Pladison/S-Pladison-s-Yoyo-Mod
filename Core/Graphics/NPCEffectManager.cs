using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Core.Graphics.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Core.Graphics
{
    /// <summary>
    /// Предоставляет функционал для управления и применения специальных визуальных эффектов к NPC.
    /// </summary>
    public static class NPCEffectManager
    {
        /// <summary>
        /// Общий 'Effect', используемый всеми типами визульных эффектов.
        /// </summary>
        private static readonly LazyAsset<Effect> _effect = LazyAsset<Effect>.From($"{nameof(SPYoyoMod)}/Assets/NPCEffects");

        // [Обводка]

        /// <summary>
        /// Параметры эффекта обводки NPC.
        /// </summary>
        public readonly ref struct OutlineSettings
        {
            /// <summary>
            /// Идентификатор NPC, к которому закреплен эффект.
            /// </summary>
            public readonly int NpcWhoAmI { get; init; }

            /// <summary>
            /// Время действия эффекта.
            /// </summary>
            public readonly int LifeTime { get; init; }

            /// <summary>
            /// Толщина обводки.
            /// </summary>
            public readonly Func<float, float> OutlineThickness { get; init; }

            /// <summary>
            /// Цвет обводки.
            /// </summary>
            public readonly Func<float, Color> OutlineColor { get; init; }

            /// <summary>
            /// Цвет заливки внутри обводки поверх NPC.
            /// </summary>
            public readonly Func<float, Color> NpcColor { get; init; }
        };

        /// <summary>
        /// Применяет эффект обводки к NPC. Не рекомендую устанавливать обводку на продолжительное время.
        /// Полсекунды будет вполне достаточно.
        /// </summary>
        public static void Outline(in OutlineSettings settings)
        {
            ModContent.GetInstance<NPCOutlineManager>()?.Outline(settings);
        }

        /// <summary>
        /// Менеджен, управляющий эффектом обводки NPC.
        /// </summary>
        [Autoload(Side = ModSide.Client)]
        private sealed class NPCOutlineManager : ILoadable
        {
            /// <summary>
            /// Контекст активного эффекта.
            /// </summary>
            public sealed class OutlineContext(in OutlineSettings settings, int timeLeft)
            {
                public readonly int NpcWhoAmI = settings.NpcWhoAmI;
                public readonly int LifeTime = settings.LifeTime;
                public readonly Func<float, float> OutlineThickness = settings.OutlineThickness;
                public readonly Func<float, Color> OutlineColor = settings.OutlineColor;
                public readonly Func<float, Color> NpcColor = settings.NpcColor;

                public int TimeLeft = timeLeft;
            }

            /// <summary>
            /// Максимальное кол-во NPC, которые могут иметь эффект обводки за раз.
            /// </summary>
            public const int MaxOutlinedNPC = 3;

            /// <summary>
            /// Список всех активных контекстов.
            /// </summary>
            private readonly List<OutlineContext> _contexts = new(MaxOutlinedNPC);

            /// <summary>
            /// Коллекция экранных целей рендеринга. Каждая цель - отдельный NPC/контекст.
            /// </summary>
            private readonly ScreenRenderTarget[] _renderTargets =
            [
                ScreenRenderTarget.Create(ScreenRenderTargetScale.Default),
                ScreenRenderTarget.Create(ScreenRenderTargetScale.Default),
                ScreenRenderTarget.Create(ScreenRenderTargetScale.Default)
            ];

            void ILoadable.Load(Mod mod)
            {
                ModEvents.OnPostUpdateEverything += Update;
                ModEvents.OnPostUpdateCameraPosition += RenderNPCToTargets;

                On_Main.DrawNPCs += (orig, main, behindTiles) =>
                {
                    orig(main, behindTiles);
                    DrawTargetsToScreen(behindTiles);
                };
            }

            void ILoadable.Unload()
            {
                ModEvents.OnPostUpdateCameraPosition -= RenderNPCToTargets;
                ModEvents.OnPostUpdateEverything -= Update;
            }

            /// <summary>
            /// Накладываем эффект обводка с определенными параметрами.
            /// </summary>
            public void Outline(in OutlineSettings settings)
            {
                if (_contexts.Count >= MaxOutlinedNPC)
                    _contexts.RemoveAt(0);

                _contexts.Add(new(in settings, settings.LifeTime));
            }

            /// <summary>
            /// Обновление активных контекстов. Удаляет контекст, если время жизни стало <= 0, а также если NPC перестал существовать.
            /// </summary>
            private void Update()
            {
                for (int i = 0; i < _contexts.Count; i++)
                {
                    var context = _contexts[i];

                    if (--context.TimeLeft <= 0 || Main.npc[context.NpcWhoAmI] is null || !Main.npc[context.NpcWhoAmI].active)
                    {
                        _contexts.RemoveAt(i);
                        i--;
                    }
                }
            }

            /// <summary>
            /// Отрисовываем контексты на соответствующие им цели рендеринга.
            /// </summary>
            private void RenderNPCToTargets()
            {
                if (_contexts.Count == 0)
                    return;

                var device = Main.graphics.GraphicsDevice;

                for (int i = 0; i < _contexts.Count; i++)
                {
                    device.SetRenderTarget(_renderTargets[i]);
                    device.Clear(Color.Transparent);

                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, GameMatrices.Transform);
                    NPCUtils.DrawNPC(Main.npc[_contexts[i].NpcWhoAmI]);
                    Main.spriteBatch.End();
                }

                device.SetRenderTarget(null);
            }

            /// <summary>
            /// Отрисовываем цели рендеринга соответствующих контекстов. 
            /// </summary>
            private void DrawTargetsToScreen(bool behindTiles)
            {
                if (_contexts.Count == 0)
                    return;

                _effect.Prepare(parameters =>
                {
                    parameters["ScreenSize"].SetValue(Main.ScreenSize.ToVector2());
                    parameters["Zoom"].SetValue(Main.GameViewMatrix.Zoom);
                });

                Main.spriteBatch.End(out var spriteBatchSnapshot);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

                for (int i = 0; i < _contexts.Count; i++)
                {
                    var context = _contexts[i];

                    if (Main.npc[context.NpcWhoAmI].behindTiles != behindTiles)
                        continue;

                    var lifeTimeRatio = 1f - context.TimeLeft / (float)context.LifeTime;
                    var outlineThickness = (context.OutlineThickness is not null) ? context.OutlineThickness(lifeTimeRatio) : 1.5f;
                    var outlineColor = (context.OutlineColor is not null) ? context.OutlineColor(lifeTimeRatio) : Color.White;
                    var npcColor = (context.NpcColor is not null) ? context.NpcColor(lifeTimeRatio) : (outlineColor * 0.4f);

                    _effect.Prepare(parameters =>
                    {
                        parameters["OutlineThickness"].SetValue(outlineThickness);
                        parameters["OutlineColor"].SetValue(outlineColor.ToVector4());
                        parameters["NPCColor"].SetValue(npcColor.ToVector4());
                    }
                    ).Apply("Outline");

                    Main.spriteBatch.Draw(_renderTargets[i], Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(spriteBatchSnapshot);
            }
        }
    }
}
