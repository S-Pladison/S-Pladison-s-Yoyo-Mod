using System.Runtime.CompilerServices;

namespace SPYoyoMod.Utils
{
    public static class GeneralUtils
    {
        /// <summary>
        /// Преобразует количество секунд (целое число) в количество тиков. 
        /// Считается, что в одной секунде 60 тиков.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SecondsToTicks(int seconds)
            => seconds * 60;

        /// <summary>
        /// Преобразует количество секунд (вещественное число) в количество тиков. 
        /// Считается, что в одной секунде 60 тиков.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SecondsToTicks(float seconds)
            => (int)(seconds * 60);

        /// <summary>
        /// Преобразует количество тиков (целое число) в количество секунд. 
        /// Считается, что в одной секунде 60 тиков.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TicksToSeconds(int ticks)
            => ticks / 60;

        /// <summary>
        /// Преобразует количество тиков (вещественное число) в количество секунд. 
        /// Считается, что в одной секунде 60 тиков.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TicksToSeconds(float ticks)
            => (int)(ticks / 60);

        /// <summary>
        /// Ничего не делает.
        /// Может использоваться как заглушка или для передачи в вызовы, 
        /// где требуется делегат без выполнения какого-либо действия.
        /// </summary>
        public static void EmptyAction() { }

        /// <inheritdoc cref="EmptyAction"/>
        public static void EmptyAction<T>(T _) { }

        /// <inheritdoc cref="EmptyAction"/>
        public static void EmptyAction<T1, T2>(T1 _1, T2 _2) { }
    }
}