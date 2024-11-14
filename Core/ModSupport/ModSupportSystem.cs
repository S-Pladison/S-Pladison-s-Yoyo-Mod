using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Terraria.ModLoader;

namespace SPYoyoMod.Core.ModSupport
{
    [LoadPriority(sbyte.MaxValue)]
    public abstract class ModSupportSystem<TMe>(string internalName = default) : ModSystem where TMe : ModSupportSystem<TMe>
    {
        private readonly string _potentialInternalName = internalName;

        public static Mod Instance { get; private set; }
        public static Assembly Code { get => Instance?.Code ?? null; }
        public static bool IsLoaded { get => Instance != null; }

        public sealed override bool IsLoadingEnabled(Mod mod)
        {
            if (TryGetSupportedMod(GetSupportedModNames(), out var supportedMod))
            {
                Instance = supportedMod;
                return true;
            }

            Instance = null;
            return false;
        }

        private ReadOnlyCollection<string> GetSupportedModNames()
        {
            var type = typeof(TMe);
            var modNameList = new List<string>(3);

            // Точное внутреннее имя мода, которое мы ввели в конструкторе
            if (!String.IsNullOrEmpty(_potentialInternalName))
                modNameList.Add(_potentialInternalName);

            const string postfix = "Support";

            // Потенциальное имя мода на основе имени типа, но без постфикса
            if (type.Name.EndsWith(postfix))
                modNameList.Add(type.Name[..^postfix.Length]);

            // Потенциальное имя мода на основе имени типа
            modNameList.Add(type.Name);

            return modNameList.AsReadOnly();
        }

        private static bool TryGetSupportedMod(ReadOnlyCollection<string> internalModNames, out Mod mod)
        {
            foreach (var internalName in internalModNames)
            {
                if (ModLoader.TryGetMod(internalName, out mod))
                    return true;
            }

            mod = null;
            return false;
        }

        public sealed override void Load()
        {
            OnLoad();
        }

        public sealed override void Unload()
        {
            OnUnload();

            Instance = null;
        }

        protected virtual void OnLoad() { }
        protected virtual void OnUnload() { }

        public static object Call(params object[] args)
        {
            if (!IsLoaded)
                return null;

            try
            {
                var value = Instance.Call(args);

                if (value is Exception ex)
                {
                    ModContent.GetInstance<SPYoyoMod>().Logger.Error(ex);
                    return null;
                }

                return value;
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<SPYoyoMod>().Logger.Error(ex);
            }

            return null;
        }

        public static bool TryCall<T>(out T value, params object[] args)
        {
            if (Call(args) is T localValue)
            {
                value = localValue;
                return true;
            }

            value = default;
            return false;
        }
    }
}