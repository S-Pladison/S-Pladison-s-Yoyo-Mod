using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.CameraModifiers;
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
        // [Направленная тряска]

        public record struct PunchSettings(
            Vector2 Position,
            Vector2 Direction,
            float Strength = 7f,
            float VibrationCyclesPerSecond = 6f,
            float DistanceFalloff = -1f,
            int Frames = 15,
            string UniqueIdentity = null
        );

        /// <summary>
        /// Создаёт эффект направленного дрожания экрана.
        /// Эффект подходит для создания иллюзии вибрации, происходящих при событиях, таких как взрывы, мощные удары и т.д.
        /// </summary>
        public static void Punch(in PunchSettings settings)
        {
            Main.instance.CameraModifiers.Add(
                new PunchCameraModifier(settings.Position, settings.Direction, settings.Strength, settings.VibrationCyclesPerSecond, settings.Frames, settings.DistanceFalloff, settings.UniqueIdentity)
            );
        }

        // Это копия ванильного класса, но, в отличии от него, данная реализация не обновляется, если игра находится *на паузе* ...
        private sealed class PunchCameraModifier(Vector2 startPosition, Vector2 direction, float strength, float vibrationCyclesPerSecond, int frames, float distanceFalloff, string uniqueIdentity) : ICameraModifier
        {
            private readonly int _framesToLast = frames;
            private readonly float _distanceFalloff = distanceFalloff;
            private readonly float _strength = strength;
            private readonly float _vibrationCyclesPerSecond = vibrationCyclesPerSecond;

            private Vector2 _startPosition = startPosition;
            private Vector2 _direction = direction;
            private int _framesLasted;
            private uint _lastUpdateTick;

            public string UniqueIdentity { get; private set; } = uniqueIdentity;
            public bool Finished { get; private set; }

            public void Update(ref CameraInfo cameraInfo)
            {
                if (_lastUpdateTick == Main.GameUpdateCount)
                    return;

                var num = (float)Math.Cos(_framesLasted / 60f * _vibrationCyclesPerSecond * (MathF.PI * 2f));
                var num2 = Terraria.Utils.Remap(_framesLasted, 0f, _framesToLast, 1f, 0f);
                var num3 = Terraria.Utils.Remap(Vector2.Distance(_startPosition, cameraInfo.OriginalCameraCenter), 0f, _distanceFalloff, 1f, 0f);

                if (_distanceFalloff == -1f)
                    num3 = 1f;

                cameraInfo.CameraPosition += _direction * num * _strength * num2 * num3;
                _framesLasted++;

                if (_framesLasted >= _framesToLast)
                    Finished = true;

                _lastUpdateTick = Main.GameUpdateCount;
            }
        }

        // [Вспышка]

        public record struct FlashSettings(
            float Strength = 0.1f,
            int Frames = 15,
            Vector2? Position = null
        );

        /// <summary>
        /// Создает эффект вспышки.
        /// Эффект может сильно напрягать глаза при высоких значениях силы, так что лучше с этим не перебарщивать.
        /// </summary>
        public static void Flash(in FlashSettings settings)
        {
            if (Main.DisableIntenseVisualEffects)
                return;

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