using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    /// <summary>
    /// This interface allows you to give dye to armor or accessory.
    /// </summary>
    public interface IDyeableEquipmentItem
    {
        public static readonly GlobalHookList<GlobalItem> Hook =
            ItemLoader.AddModHook(
                new GlobalHookList<GlobalItem>(typeof(IDyeableEquipmentItem).GetMethod(nameof(UpdateDye)))
            );

        /// <summary>
        /// Allows you to give dye to this armor or accessory.
        /// </summary>
        void UpdateDye(Item item, int dye, Player player, bool isNotInVanitySlot, bool isSetToHidden);

        private class UpdateDyeImplementation : ILoadable
        {
            public void Load(Mod mod)
            {
                On_Player.UpdateItemDye += (orig, player, isNotInVanitySlot, isSetToHidden, armorItem, dyeItem) =>
                {
                    orig(player, isNotInVanitySlot, isSetToHidden, armorItem, dyeItem);

                    if (armorItem.IsAir) return;

                    (armorItem.ModItem as IDyeableEquipmentItem)?.UpdateDye(armorItem, dyeItem.dye, player, isNotInVanitySlot, isSetToHidden);

                    foreach (IDyeableEquipmentItem g in Hook.Enumerate(armorItem))
                    {
                        g.UpdateDye(armorItem, dyeItem.dye, player, isNotInVanitySlot, isSetToHidden);
                    }
                };
            }

            public void Unload() { }
        }
    }
}