using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static SPYoyoMod.ModSets;

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

        public override void SetupTravelShop(int[] shop, ref int nextSlot)
        {
            ModifyTravellingMerchantShop(shop, ref nextSlot);
        }

        private static void ModifySkeletonMerchantShop(NPCShop shop)
        {
            if (shop.TryGetEntry(ItemID.Gradient, out var entry))
                entry.Disable();
        }

        private static void ModifyTravellingMerchantShop(int[] shop, ref int nextSlot)
        {
            var hasCode1 = false;
            var code2Index = -1;

            for (var i = 0; i < nextSlot; i++)
            {
                if (shop[i] == ItemID.Code1)
                    hasCode1 = true;
                else if (shop[i] == ItemID.Code2)
                    code2Index = i;
            }

            if (code2Index < 0)
                return;

            if (!hasCode1)
            {
                shop[code2Index] = ItemID.Code1;
                return;
            }

            for (var i = code2Index + 1; i < nextSlot; i++)
                shop[i - 1] = shop[i];

            shop[--nextSlot] = 0;
        }
    }
}