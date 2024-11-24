using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public sealed class ShopGlobalNPC : GlobalNPC
    {
        public override void ModifyShop(NPCShop shop)
        {
            switch (shop.NpcType)
            {
                case NPCID.SkeletonMerchant:
                    ModifySkeletonMerchantShop(shop);
                    break;
            }
        }

        private static void ModifySkeletonMerchantShop(NPCShop shop)
        {
            // Убираем из ассортимента Gradient, т.к. ему был добавлен рецепт крафта
            if (shop.TryGetEntry(ItemID.Gradient, out var entry))
            {
                entry.Disable();
            }
        }
    }
}