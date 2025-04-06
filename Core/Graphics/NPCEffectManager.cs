using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
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
        // [Обводка]

        public record struct OutlineSettings(
            int NpcWhoAmI,
            int LifeTime,
            Func<float, float> OutlineThickness,
            Func<float, Color> OutlineColor,
            Func<float, Color> NpcColor
        );

        /// <summary>
        /// Применяет эффект обводки к NPC. Не рекомендую устанавливать обводку на продолжительное время.
        /// Полсекунды будет вполне достаточно.
        /// </summary>
        public static void Outline(in OutlineSettings settings)
        {
            ModContent.GetInstance<NPCOutlineManager>()?.Outline(settings);
        }

        [Autoload(Side = ModSide.Client)]
        private sealed class NPCOutlineManager : ILoadable
        {
            public class OutlineData(in OutlineSettings settings, int timeLeft)
            {
                public OutlineSettings Settings = settings;
                public int TimeLeft = timeLeft;
            }

            public const int MaxOutlinedNPC = 3;

            private readonly List<OutlineData> _outlineData = new(MaxOutlinedNPC);
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

            public void Outline(in OutlineSettings settings)
            {
                if (_outlineData.Count >= MaxOutlinedNPC)
                    _outlineData.RemoveAt(0);

                _outlineData.Add(new(settings, settings.LifeTime));
            }

            private void Update()
            {
                for (int i = 0; i < _outlineData.Count; i++)
                {
                    var data = _outlineData[i];

                    if (--data.TimeLeft <= 0 || Main.npc[data.Settings.NpcWhoAmI] is null || !Main.npc[data.Settings.NpcWhoAmI].active)
                    {
                        _outlineData.RemoveAt(i);
                        i--;
                    }
                }
            }

            private void RenderNPCToTargets()
            {
                if (_outlineData.Count == 0)
                    return;

                var device = Main.graphics.GraphicsDevice;

                for (int i = 0; i < _outlineData.Count; i++)
                {
                    device.SetRenderTarget(_renderTargets[i]);
                    device.Clear(Color.Transparent);

                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, GameMatrices.Effect);
                    NPCUtils.DrawNPC(Main.npc[_outlineData[i].Settings.NpcWhoAmI]);
                    Main.spriteBatch.End();
                }

                device.SetRenderTarget(null);
            }

            private void DrawTargetsToScreen(bool behindTiles)
            {
                if (_outlineData.Count == 0)
                    return;

                var effect = NPCEffectAssets.Effect.Prepare(parameters =>
                {
                    parameters["ScreenSize"].SetValue(Main.ScreenSize.ToVector2());
                });

                Main.spriteBatch.End(out var spriteBatchSnapshot);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, GameMatrices.Zoom);

                for (int i = 0; i < _outlineData.Count; i++)
                {
                    var data = _outlineData[i];
                    var settings = data.Settings;

                    if (Main.npc[settings.NpcWhoAmI].behindTiles != behindTiles)
                        continue;

                    var lifeTimeRatio = 1f - data.TimeLeft / (float)settings.LifeTime;
                    var outlineThickness = (settings.OutlineThickness is not null) ? settings.OutlineThickness(lifeTimeRatio) : 1.5f;
                    var outlineColor = (settings.OutlineColor is not null) ? settings.OutlineColor(lifeTimeRatio) : Color.White;
                    var npcColor = (settings.NpcColor is not null) ? settings.NpcColor(lifeTimeRatio) : (outlineColor * 0.4f);

                    effect
                        .Prepare(parameters =>
                        {
                            parameters["OutlineThickness"].SetValue(outlineThickness);
                            parameters["OutlineColor"].SetValue(outlineColor.ToVector4());
                            parameters["NPCColor"].SetValue(npcColor.ToVector4());
                        })
                        .Apply("Outline");

                    Main.spriteBatch.Draw(_renderTargets[i], Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(spriteBatchSnapshot);
            }
        }

        [Autoload(Side = ModSide.Client)]
        private sealed class NPCEffectAssets : ILoadable
        {
            public static Asset<Effect> Effect { get; private set; } = ModContent.Request<Effect>($"{nameof(SPYoyoMod)}/Assets/NPCEffects");

            void ILoadable.Unload()
            {
                Effect = null;
            }

            void ILoadable.Load(Mod mod) { }
        }
    }
}
