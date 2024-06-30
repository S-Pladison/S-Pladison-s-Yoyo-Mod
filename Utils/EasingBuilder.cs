using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

namespace SPYoyoMod.Utils
{
    public class EasingBuilder
    {
        private EasingData[] _easings;
        private float[] _shiftedDurations;
        private float _totalDuration;
        private int _addedEasingCount;

        public EasingBuilder(int? easingCount = null)
        {
            if (easingCount is null || easingCount.Value <= 0)
            {
                _easings = Array.Empty<EasingData>();
                _shiftedDurations = Array.Empty<float>();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EasingBuilder Add(EasingFunctions.EasingDelegate easing, float duration, float startY, float endY)
            => Add(new EasingData(easing, duration, startY, endY));

        public EasingBuilder Add(EasingData easing)
        {
            if (easing.Duration <= 0)
                throw new ArgumentException($"{nameof(easing.Duration)} must be greater than 0");

            if (_addedEasingCount >= _easings.Length)
                ResizeArrays(_addedEasingCount + 1);

            _totalDuration += easing.Duration;

            _shiftedDurations[_addedEasingCount] = _totalDuration;
            _easings[_addedEasingCount] = easing;

            _addedEasingCount++;

            return this;
        }

        public float Evaluate(float t)
        {
            if (_easings.Length == 0)
                return 0f;

            if (t <= 0f)
                return _easings[0].StartY;

            if (t >= 1f)
                return _easings[_addedEasingCount - 1].EndY;

            var progress = t * _totalDuration;
            var easingIndex = 0;

            for (var i = 0; i < _addedEasingCount; i++)
            {
                if (progress > _shiftedDurations[i])
                    continue;

                easingIndex = i;
                break;
            }

            ref var easingData = ref _easings[easingIndex];

            return MathHelper.Lerp(easingData.StartY, easingData.EndY, (progress - _shiftedDurations[easingIndex] + easingData.Duration) / easingData.Duration);
        }

        private void ResizeArrays(int size)
        {
            Array.Resize(ref _easings, size);
            Array.Resize(ref _shiftedDurations, size);
        }

        public record struct EasingData(EasingFunctions.EasingDelegate Easing, float Duration, float StartY, float EndY)
        {
            public static implicit operator EasingData((EasingFunctions.EasingDelegate easing, float duration, float startY, float endY) tuple)
                => new(tuple.easing, tuple.duration, tuple.startY, tuple.endY);
        }
    }
}