using SPYoyoMod.Core;
using System;
using System.Runtime.CompilerServices;
using Terraria.ModLoader;

namespace SPYoyoMod
{
    // Спросите меня, почему я не пихнул свойство Logger для доступа к объекту в основном классе (чтот типо SPYoyoMod.Logger.Info(...) ).
    // А я отвечу, что ModLogger.Info(...) выглядит лучше...
    [LoadBefore]
    public sealed class ModLogger : ILoadable
    {
        private static Mod _modInstance = null;

        public static Mod Mod => _modInstance ??= ModLoader.GetMod(typeof(SPYoyoMod).Name);

        void ILoadable.Load(Mod mod) { }

        void ILoadable.Unload()
        {
            _modInstance = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(object message)
            => Mod.Logger.Debug(message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(object message, Exception exception)
            => Mod.Logger.Debug(message, exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DebugFormat(string format, params object[] args)
            => Mod.Logger.DebugFormat(format, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DebugFormat(string format, object arg0)
            => Mod.Logger.DebugFormat(format, arg0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DebugFormat(string format, object arg0, object arg1)
            => Mod.Logger.DebugFormat(format, arg0, arg1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DebugFormat(string format, object arg0, object arg1, object arg2)
            => Mod.Logger.DebugFormat(format, arg0, arg1, arg2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DebugFormat(IFormatProvider provider, string format, params object[] args)
            => Mod.Logger.DebugFormat(provider, format, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(object message)
            => Mod.Logger.Info(message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(object message, Exception exception)
            => Mod.Logger.Info(message, exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InfoFormat(string format, params object[] args)
            => Mod.Logger.InfoFormat(format, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InfoFormat(string format, object arg0)
            => Mod.Logger.InfoFormat(format, arg0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InfoFormat(string format, object arg0, object arg1)
            => Mod.Logger.InfoFormat(format, arg0, arg1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InfoFormat(string format, object arg0, object arg1, object arg2)
            => Mod.Logger.InfoFormat(format, arg0, arg1, arg2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InfoFormat(IFormatProvider provider, string format, params object[] args)
            => Mod.Logger.InfoFormat(provider, format, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warn(object message)
            => Mod.Logger.Warn(message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warn(object message, Exception exception)
            => Mod.Logger.Warn(message, exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WarnFormat(string format, params object[] args)
            => Mod.Logger.WarnFormat(format, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WarnFormat(string format, object arg0)
            => Mod.Logger.WarnFormat(format, arg0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WarnFormat(string format, object arg0, object arg1)
            => Mod.Logger.WarnFormat(format, arg0, arg1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WarnFormat(string format, object arg0, object arg1, object arg2)
            => Mod.Logger.WarnFormat(format, arg0, arg1, arg2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WarnFormat(IFormatProvider provider, string format, params object[] args)
            => Mod.Logger.WarnFormat(provider, format, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(object message)
            => Mod.Logger.Error(message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(object message, Exception exception)
            => Mod.Logger.Error(message, exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ErrorFormat(string format, params object[] args)
            => Mod.Logger.ErrorFormat(format, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ErrorFormat(string format, object arg0)
            => Mod.Logger.ErrorFormat(format, arg0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ErrorFormat(string format, object arg0, object arg1)
            => Mod.Logger.ErrorFormat(format, arg0, arg1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ErrorFormat(string format, object arg0, object arg1, object arg2)
            => Mod.Logger.ErrorFormat(format, arg0, arg1, arg2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ErrorFormat(IFormatProvider provider, string format, params object[] args)
            => Mod.Logger.ErrorFormat(provider, format, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fatal(object message)
            => Mod.Logger.Fatal(message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fatal(object message, Exception exception)
            => Mod.Logger.Fatal(message, exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FatalFormat(string format, params object[] args)
            => Mod.Logger.FatalFormat(format, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FatalFormat(string format, object arg0)
            => Mod.Logger.FatalFormat(format, arg0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FatalFormat(string format, object arg0, object arg1)
            => Mod.Logger.FatalFormat(format, arg0, arg1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FatalFormat(string format, object arg0, object arg1, object arg2)
            => Mod.Logger.FatalFormat(format, arg0, arg1, arg2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FatalFormat(IFormatProvider provider, string format, params object[] args)
            => Mod.Logger.FatalFormat(provider, format, args);
    }
}
