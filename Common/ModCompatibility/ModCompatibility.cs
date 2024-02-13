using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.ModCompatibility
{
    public abstract class ModCompatibility : ILoadable
    {
        public abstract string ModName { get; }
        public Mod Mod { get; private set; }
        public bool IsModLoaded { get; private set; }
        public Assembly Assembly { get => Mod.Code; }

        public virtual void Load() { }
        public virtual void Unload() { }

        public bool TryGetMod(out Mod mod)
        {
            if (!IsModLoaded)
            {
                mod = null;
                return false;
            }

            mod = Mod;
            return true;
        }

        void ILoadable.Load(Mod _)
        {
            if (!ModLoader.TryGetMod(ModName, out var mod))
            {
                IsModLoaded = false;
                Mod = null;
                return;
            }

            Mod = mod;
            IsModLoaded = true;

            Load();
        }

        public void AddHook(string typePath, string methodName, Delegate hookDelegate)
        {
            AddHook(typePath, methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance, hookDelegate);
        }

        public void AddHook(string typePath, string methodName, BindingFlags bindingFlags, Delegate hookDelegate)
        {
            if (!IsModLoaded) return;

            var typeInfo = Assembly.GetType($"{ModName}.{typePath}");

            if (typeInfo is null)
                ModContent.GetInstance<SPYoyoMod>().Logger.Error($"Error:[Failed to add hook] Mod:[{ModName}] Type:[{typePath}]:[null]");

            var methodInfo = typeInfo.GetMethod(methodName, bindingFlags);

            if (methodInfo is null)
                ModContent.GetInstance<SPYoyoMod>().Logger.Error($"Error:[Failed to add hook] Mod:[{ModName}] Type:[{typePath}] Method:[{methodName}]:[null]");

            MonoModHooks.Add(methodInfo, hookDelegate);
        }

        public void AddILHook(string typePath, string methodName, ILContext.Manipulator callback)
        {
            AddILHook(typePath, methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance, callback);
        }

        public void AddILHook(string typePath, string methodName, BindingFlags bindingFlags, ILContext.Manipulator callback)
        {
            if (!IsModLoaded) return;

            var typeInfo = Assembly.GetType($"{ModName}.{typePath}");

            if (typeInfo is null)
                ModContent.GetInstance<SPYoyoMod>().Logger.Error($"Error:[Modification failed] Mod:[{ModName}] Type:[{typePath}]:[null]");

            var methodInfo = typeInfo.GetMethod(methodName, bindingFlags);

            if (methodInfo is null)
                ModContent.GetInstance<SPYoyoMod>().Logger.Error($"Error:[Modification failed] Mod:[{ModName}] Type:[{typePath}] Method:[{methodName}]:[null]");

            MonoModHooks.Modify(methodInfo, callback);
        }
    }
}