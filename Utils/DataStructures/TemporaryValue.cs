using System.Runtime.CompilerServices;

namespace SPYoyoMod.Utils
{
    /// <summary>
    /// Временно подменяет значение и восстанавливает исходное при Dispose.
    /// <br/>Предназначен для использования в блоке <c>using</c>.
    /// <br/>Для подмены значения используй функцию <see cref="TemporaryValue.Replace{T}(ref T, T)"/>.
    /// </summary>
    public readonly ref struct TemporaryValue<T>
    {
        private readonly ref T _location;
        private readonly T _original;

        /// <summary>
        /// Подменяет значение <paramref name="location"/> на <paramref name="temporary"/>.
        /// Исходное значение будет восстановлено при вызове <see cref="Dispose"/>.
        /// </summary>
        public TemporaryValue(ref T location, T temporary)
        {
            _location = ref location;
            _original = location;
            location = temporary;
        }

        /// <summary>
        /// Восстанавливает исходное значение.
        /// </summary>
        public void Dispose()
            => _location = _original;
    }

    /// <summary>
    /// Вспомогательные методы для создания <see cref="TemporaryValue{T}"/>.
    /// </summary>
    public static class TemporaryValue
    {
        /// <summary>
        /// Временно подменяет значение <paramref name="location"/> на <paramref name="temporary"/>.
        /// Исходное значение будет восстановлено при Dispose.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TemporaryValue<T> Replace<T>(ref T location, T temporary)
            => new(ref location, temporary);
    }
}
