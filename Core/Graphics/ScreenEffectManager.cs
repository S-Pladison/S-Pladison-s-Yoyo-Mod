using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace SPYoyoMod.Core.Graphics
{
    /// <summary>
    /// Менеджер, отвечающий за работу с простыми экранными эффектами.
    /// </summary>
    public static class ScreenEffectManager
    {
        // [Вспышка]

        public record struct FlashSettings(
            float Strength = 0.1f,
            int Frames = 15,
            Vector2? Position = null
        );

        /// <summary>
        /// Создает эффект вспышки.
        /// Эффект может сильно напрягать глаза при высоких значениях силы, так что лучше с этим не перебарщивать.
        /// Для светочувствительных людей этот эффект можно отключить в конфиге мода.
        /// </summary>
        public static void Flash(in FlashSettings settings)
        {
            ModContent.GetInstance<ScreenFlashManager>()?.Flash(settings);
        }

        [Autoload(Side = ModSide.Client)]
        private sealed class ScreenFlashManager : ILoadable
        {
            public const string FilterName = $"{nameof(SPYoyoMod)}:Flash";

            private int _flashInitTime;
            private int _flashTime;
            private float _flashStrength;
            private Vector2? _flashPosition;

            void ILoadable.Load(Mod mod)
            {
                Filters.Scene[FilterName] = new Filter(
                    new ScreenShaderData(ModContent.Request<Effect>($"{nameof(SPYoyoMod)}/Assets/ScreenEffect_Flash"), $"ScreenFlash"), EffectPriority.VeryHigh
                );

                ModEvents.OnPostUpdateEverything += Update;
            }

            void ILoadable.Unload()
            {
                ModEvents.OnPostUpdateEverything -= Update;
            }

            public void Flash(in FlashSettings settings)
            {
                _flashStrength = MathHelper.Clamp(settings.Strength, 0, 1);
                _flashInitTime = (int)MathHelper.Max(settings.Frames, 0);
                _flashTime = _flashInitTime;
                _flashPosition = settings.Position;
            }

            private void Update()
            {
                if (_flashTime > 0f)
                {
                    Filters.Scene.Activate(FilterName);
                    Filters.Scene[FilterName]
                        .GetShader()
                        .UseIntensity(_flashTime / (float)_flashInitTime * _flashStrength)
                        .UseTargetPosition(_flashPosition ?? (Main.screenPosition + Main.ScreenSize.ToVector2() * 0.5f));

                    _flashTime--;
                }
                else if (Filters.Scene[FilterName].IsActive())
                {
                    Filters.Scene[FilterName].GetShader().UseIntensity(0f);
                    Filters.Scene[FilterName].Deactivate();
                }
            }
        }

        // [Виньетка]

        /// TODO: Реализовать :p

        // [Хроматическая аберрация]

        /// TODO: Реализовать :З
    }
}