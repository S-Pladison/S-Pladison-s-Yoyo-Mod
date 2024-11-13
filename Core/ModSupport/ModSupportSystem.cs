using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;

namespace SPYoyoMod.Core.ModSupport
{
    [LoadPriority(sbyte.MaxValue)]
    public abstract class ModSupportSystem<TMe>(string internalName = default) : ModSystem where TMe : ModSupportSystem<TMe>
    {
        public class SupportedModData(Mod instance, string internalName)
        {
            public readonly Mod Instance = instance;
            public readonly string InternalName = internalName;
        }

        private readonly string _potentialInternalName = internalName;

        public static SupportedModData Data { get; private set; }
        public static Assembly Code { get => Data?.Instance.Code ?? null; }
        public static bool IsModLoaded { get => Data != null; }

        public sealed override bool IsLoadingEnabled(Mod mod)
        {
            if (TryGetSupportedMod(GetSupportedModNames<TMe>(), out var data))
            {
                Data = data;
                return true;
            }

            Data = null;
            return false;
        }

        private List<string> GetSupportedModNames<T>() where T : ModSupportSystem<T>
        {
            var type = typeof(T);
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

            return modNameList;
        }

        private static bool TryGetSupportedMod(IList<string> internalModNames, out SupportedModData data)
        {
            foreach (var internalName in internalModNames)
            {
                if (ModLoader.TryGetMod(internalName, out Mod mod))
                {
                    data = new SupportedModData(mod, internalName);
                    return true;
                }
            }

            data = new SupportedModData(null, internalModNames.First());
            return false;
        }

        public static object Call(params object[] args)
        {
            if (!IsModLoaded)
                return null;

            try
            {
                var value = Data.Instance.Call(args);

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