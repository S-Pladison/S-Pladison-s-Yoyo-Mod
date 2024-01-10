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
            if (!ModLoader.TryGetMod(ModName, out Mod mod))
            {
                IsModLoaded = false;
                Mod = null;
                return;
            }

            Mod = mod;
            IsModLoaded = true;

            Load();
        }
    }
}