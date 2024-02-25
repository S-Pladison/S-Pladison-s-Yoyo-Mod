using System;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    /// <summary>
    /// Class of events useful for mod.
    /// </summary>
    public class NPCEvents : ILoadable
    {
        /// <summary>
        /// Allows you to add and modify global loot rules that are conditional, i.e.vanilla's biome keys and souls.
        /// </summary>
        public static event Action<GlobalLoot> OnModifyGlobalLoot;

        void ILoadable.Load(Mod mod)
        {
            OnModifyGlobalLoot += (_) => { };
        }

        void ILoadable.Unload()
        {
            OnModifyGlobalLoot = null;
        }

        private class EventGlobalNPC : GlobalNPC
        {
            public override void ModifyGlobalLoot(GlobalLoot globalLoot) => OnModifyGlobalLoot(globalLoot);
        }
    }
}