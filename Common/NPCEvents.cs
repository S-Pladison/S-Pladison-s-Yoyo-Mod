using System;
using Terraria;
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

        /// <summary>
        /// Allows you to make the NPC either regenerate health or take damage over time
        /// by setting npc.lifeRegen. Regeneration or damage will occur at a rate of half
        /// of npc.lifeRegen per second. The damage parameter is the number that appears
        /// above the NPC's head if it takes damage over time.
        /// </summary>
        public static event UpdateLifeRegenDelegate OnUpdateLifeRegen;

        void ILoadable.Load(Mod mod)
        {
            OnModifyGlobalLoot += (_) => { };
            OnUpdateLifeRegen += (NPC _, ref int _) => { };
        }

        void ILoadable.Unload()
        {
            OnModifyGlobalLoot = null;
            OnUpdateLifeRegen = null;
        }

        private class EventGlobalNPC : GlobalNPC
        {
            public override void ModifyGlobalLoot(GlobalLoot globalLoot) => OnModifyGlobalLoot(globalLoot);
            public override void UpdateLifeRegen(NPC npc, ref int damage) => OnUpdateLifeRegen(npc, ref damage);
        }

        public delegate void UpdateLifeRegenDelegate(NPC npc, ref int damage);
    }
}