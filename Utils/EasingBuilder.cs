using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

namespace SPYoyoMod.Utils
{
    /// <summary>
    /// Класс для построения последовательности анимаций с использованием функций сглаживания.
    /// Позволяет добавлять отдельные сегменты анимации и вычислять итоговое значение для заданного времени.
    /// </summary>
    public sealed class EasingBuilder
    {
        private EasingData[] _easings;
        private float[] _shiftedDurations;
        private float _totalDuration;
        private int _addedEasingCount;

        /// <summary>
        /// Создает новый экземпляр <see cref="EasingBuilder"/>.
        /// </summary>
        /// <param name="easingCount">Ожидаемое количество сегментов (необязательно).</param>
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

        /// <summary>
        /// Создает новый экземпляр <see cref="EasingBuilder"/> и инициализирует его указанными сегментами.
        /// </summary>
        /// <param name="easings">Набор начальных сегментов сглаживания.</param>
        public EasingBuilder(params EasingData[] easings) : this(easings.Length)
        {
            for (var i = 0; i < easings.Length; i++)
            {
                ref var easing = ref easings[i];

                Add(easing.Easing, easing.Duration, easing.StartY, easing.EndY);
            }
        }

        /// <summary>
        /// Добавляет новый сегмент сглаживания в последовательность.
        /// </summary>
        /// <param name="easing">Функция сглаживания.</param>
        /// <param name="duration">Длительность сегмента.</param>
        /// <param name="endY">Конечное значение сегмента.</param>
        /// <returns>Текущий экземпляр <see cref="EasingBuilder"/> для цепочки вызовов.</returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EasingBuilder Add(EasingFunctions.EasingDelegate easing, float duration, float endY)
            => Add(new EasingData(easing, duration, _addedEasingCount > 0 ? _easings[_addedEasingCount - 1].EndY : 0, endY));

        /// <summary>
        /// Добавляет новый сегмент сглаживания в последовательность с заданными начальными и конечными значениями.
        /// </summary>
        /// <param name="easing">Функция сглаживания.</param>
        /// <param name="duration">Длительность сегмента.</param>
        /// <param name="startY">Начальное значение сегмента.</param>
        /// <param name="endY">Конечное значение сегмента.</param>
        /// <returns>Текущий экземпляр <see cref="EasingBuilder"/> для цепочки вызовов.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EasingBuilder Add(EasingFunctions.EasingDelegate easing, float duration, float startY, float endY)
            => Add(new EasingData(easing, duration, startY, endY));

        /// <summary>
        /// Добавляет новый сегмент сглаживания в последовательность.
        /// </summary>
        /// <param name="easing">Данные сегмента сглаживания.</param>
        /// <returns>Текущий экземпляр <see cref="EasingBuilder"/> для цепочки вызовов.</returns>
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

        /// <summary>
        /// Вычисляет значение последовательности сглаживания для заданного времени.
        /// </summary>
        /// <param name="t">Нормализованное время (от 0 до 1).</param>
        /// <returns>Вычисленное значение сглаживания.</returns>
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
            var localT = (progress - _shiftedDurations[easingIndex] + easingData.Duration) / easingData.Duration;

            return MathHelper.Lerp(easingData.StartY, easingData.EndY, easingData.Easing(localT));
        }

        /// <summary>
        /// Изменяет размер внутренних массивов для хранения данных сглаживания.
        /// </summary>
        /// <param name="size">Новый размер массивов.</param>
        private void ResizeArrays(int size)
        {
            Array.Resize(ref _easings, size);
            Array.Resize(ref _shiftedDurations, size);
        }

        /// <summary>
        /// Структура, представляющая данные для одного сегмента сглаживания.
        /// </summary>
        public record struct EasingData(EasingFunctions.EasingDelegate Easing, float Duration, float StartY, float EndY)
        {
            public static implicit operator EasingData((EasingFunctions.EasingDelegate easing, float duration, float startY, float endY) tuple)
                => new(tuple.easing, tuple.duration, tuple.startY, tuple.endY);
        }
    }
}