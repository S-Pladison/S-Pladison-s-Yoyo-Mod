using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Miscellaneous
{
    public sealed class StrangeDrinkItem : ModItem
    {
        public override string Texture => $"{nameof(SPYoyoMod)}/Assets/Items/Mod.Miscellaneous/StrangeDrink_Item";

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Quest;
            Item.width = 22;
            Item.height = 46;
        }
    }
}
