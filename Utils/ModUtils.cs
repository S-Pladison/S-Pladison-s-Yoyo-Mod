using System.Runtime.CompilerServices;

namespace SPYoyoMod.Utils
{
    public static class ModUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SecondsToTicks(int seconds)
            => seconds * 60;

        public static void EmptyAction() { }
        public static void EmptyAction<T>(T _) { }
        public static void EmptyAction<T1, T2>(T1 _1, T2 _2) { }
    }
}