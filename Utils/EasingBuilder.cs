using Microsoft.Xna.Framework;
using System;

namespace SPYoyoMod.Utils
{
    public class EasingBuilder
    {
        private EasingData[] easings;
        private float[] shiftedDurations;
        private float totalDuration;
        private int addedEasingCount;

        public EasingBuilder(int? easingCount = null)
        {
            if (easingCount is null || easingCount.Value <= 0)
            {
                easings = Array.Empty<EasingData>();
                shiftedDurations = Array.Empty<float>();
                return;
            }

            ResizeArrays(easingCount.Value);
        }

        public EasingBuilder(params EasingData[] easings) : this(easings.Length)
        {
            for (var i = 0; i < easings.Length; i++)
            {
                ref var easing = ref easings[i];

                Add(easing.Easing, easing.Duration, easing.StartY, easing.EndY);
            }
        }

        public EasingBuilder Add(EasingFunctions.EasingDelegate easing, float duration, float startY, float endY)
            => Add(new EasingData(easing, duration, startY, endY));

        public EasingBuilder Add(EasingData easing)
        {
            if (easing.Duration <= 0)
                throw new ArgumentException($"{nameof(easing.Duration)} must be greater than 0");

            if (addedEasingCount >= easings.Length)
                ResizeArrays(addedEasingCount + 1);

            totalDuration += easing.Duration;

            shiftedDurations[addedEasingCount] = totalDuration;
            easings[addedEasingCount] = easing;

            addedEasingCount++;

            return this;
        }

        public float Evaluate(float t)
        {
            if (easings.Length == 0)
                return 0f;

            if (t <= 0f)
                return easings[0].StartY;

            if (t >= 1f)
                return easings[addedEasingCount - 1].EndY;

            var progress = t * totalDuration;
            var easingIndex = 0;

            for (var i = 0; i < addedEasingCount; i++)
            {
                if (progress > shiftedDurations[i])
                    continue;

                easingIndex = i;
                break;
            }

            ref var easingData = ref easings[easingIndex];

            return MathHelper.Lerp(easingData.StartY, easingData.EndY, (progress - shiftedDurations[easingIndex] + easingData.Duration) / easingData.Duration);
        }

        private void ResizeArrays(int size)
        {
            Array.Resize(ref easings, size);
            Array.Resize(ref shiftedDurations, size);
        }

        public record struct EasingData(EasingFunctions.EasingDelegate Easing, float Duration, float StartY, float EndY)
        {
            public static implicit operator EasingData((EasingFunctions.EasingDelegate easing, float duration, float startY, float endY) tuple)
                => new(tuple.easing, tuple.duration, tuple.startY, tuple.endY);
        }
    }
}