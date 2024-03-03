using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    /// <summary>
    /// Class of events useful for mod.
    /// </summary>
    public class ItemEvents : ILoadable
    {
        /// <summary>
        /// Allows you to give effects to armors and accessories, such as increased damage.
        /// </summary>
        public static event UpdateEquipDelegate OnUpdateEquip;

        void ILoadable.Load(Mod mod)
        {
            OnUpdateEquip += (_, _) => { };
        }

        void ILoadable.Unload()
        {
            OnUpdateEquip = null;
        }

        private class EventGlobalItem : GlobalItem
        {
            public override void UpdateEquip(Item item, Player player) => OnUpdateEquip(item, player);
        }

        public delegate void UpdateEquipDelegate(Item item, Player player);
    }
}