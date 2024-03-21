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
        /// Allows you to give effects to accessories. The hideVisual parameter is whether
        /// the player has marked the accessory slot to be hidden from being drawn on the player.
        /// </summary>
        public static event UpdateAccessoryDelegate OnUpdateAccessory;

        /// <summary>
        /// Allows you to give effects to armors and accessories, such as increased damage.
        /// </summary>
        public static event UpdateEquipDelegate OnUpdateEquip;

        void ILoadable.Load(Mod mod)
        {
            OnUpdateAccessory += (_, _, _) => { };
            OnUpdateEquip += (_, _) => { };
        }

        void ILoadable.Unload()
        {
            OnUpdateEquip = null;
        }

        private class EventGlobalItem : GlobalItem
        {
            public override void UpdateAccessory(Item item, Player player, bool hideVisual) => OnUpdateAccessory(item, player, hideVisual);
            public override void UpdateEquip(Item item, Player player) => OnUpdateEquip(item, player);
        }

        public delegate void UpdateAccessoryDelegate(Item item, Player player, bool hideVisual);
        public delegate void UpdateEquipDelegate(Item item, Player player);
    }
}