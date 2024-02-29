using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.CameraModifiers;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    [Autoload(Side = ModSide.Client)]
    public class ScreenEffects : ILoadable
    {
        private int flashInitTime;
        private int flashTime;
        private float flashStrength;
        private Vector2? flashPosition;

        public ScreenEffects Shake(Vector2 position, Vector2 direction, float strength, float vibrationCyclesPerSecond, int frames, float distanceFalloff = -1f, string uniqueIdentity = null)
        {
            var modifier = new ScreenEffects.PunchCameraModifier(position, direction, strength, vibrationCyclesPerSecond, frames, distanceFalloff, uniqueIdentity);

            Main.instance.CameraModifiers.Add(modifier);

            return this;
        }

        public ScreenEffects Flash(float strength, int frames, Vector2? position = null)
        {
            flashStrength = MathHelper.Clamp(strength, 0, 1);
            flashInitTime = (int)MathHelper.Max(frames, 0);
            flashTime = flashInitTime;
            flashPosition = position;

            return this;
        }

        void ILoadable.Load(Mod mod)
        {
            LoadFilters(new Ref<Effect>(ModAssets.RequestEffect("ScreenEffects").Value));

            ModEvents.OnPostUpdateEverything += UpdateFilters;
        }

        void ILoadable.Unload()
        {
            // ...
        }

        private void LoadFilters(Ref<Effect> refEffect)
        {
            LoadFilter("Flash", refEffect);
        }

        private void LoadFilter(string name, Ref<Effect> refEffect)
        {
            Filters.Scene[$"{nameof(SPYoyoMod)}:{name}"] = new Filter(new ScreenShaderData(refEffect, $"{name}Pass"), EffectPriority.VeryHigh);
        }

        private void UpdateFilters()
        {
            var filterName = $"{nameof(SPYoyoMod)}:Flash";

            if (flashTime > 0f)
            {
                Filters.Scene.Activate(filterName);
                Filters.Scene[filterName]
                    .GetShader()
                    .UseIntensity(flashTime / (float)flashInitTime * flashStrength)
                    .UseTargetPosition(flashPosition ?? (Main.screenPosition + Main.ScreenSize.ToVector2() * 0.5f));

                flashTime--;
            }
            else
            {
                Filters.Scene[filterName].GetShader().UseIntensity(0f);
                Filters.Scene[filterName].Deactivate();
            }
        }

        private class PunchCameraModifier : ICameraModifier
        {
            private readonly int framesToLast;
            private readonly float distanceFalloff;
            private readonly float strength;
            private readonly float vibrationCyclesPerSecond;

            private Vector2 startPosition;
            private Vector2 direction;
            private int framesLasted;
            private uint lastUpdateTick;

            public string UniqueIdentity { get; private set; }
            public bool Finished { get; private set; }

            public PunchCameraModifier(Vector2 startPosition, Vector2 direction, float strength, float vibrationCyclesPerSecond, int frames, float distanceFalloff = -1f, string uniqueIdentity = null)
            {
                this.startPosition = startPosition;
                this.direction = direction;
                this.strength = strength;
                this.vibrationCyclesPerSecond = vibrationCyclesPerSecond;
                this.framesToLast = frames;
                this.distanceFalloff = distanceFalloff;

                UniqueIdentity = uniqueIdentity;
            }

            public void Update(ref CameraInfo cameraInfo)
            {
                if (lastUpdateTick == Main.GameUpdateCount) return;

                var num = (float)Math.Cos(framesLasted / 60f * vibrationCyclesPerSecond * (MathF.PI * 2f));
                var num2 = Terraria.Utils.Remap(framesLasted, 0f, framesToLast, 1f, 0f);
                var num3 = Terraria.Utils.Remap(Vector2.Distance(startPosition, cameraInfo.OriginalCameraCenter), 0f, distanceFalloff, 1f, 0f);

                if (distanceFalloff == -1f)
                {
                    num3 = 1f;
                }

                cameraInfo.CameraPosition += direction * num * strength * num2 * num3;
                framesLasted++;

                if (framesLasted >= framesToLast)
                {
                    Finished = true;
                }

                lastUpdateTick = Main.GameUpdateCount;
            }
        }
    }
}