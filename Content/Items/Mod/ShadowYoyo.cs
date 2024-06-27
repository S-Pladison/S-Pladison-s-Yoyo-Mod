using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod
{
    public class ShadowYoyo : ModItem
    {
        public override string Texture => ModAssets.ItemsPath + "ShadowYoyo";

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 26;
            Item.rare = ItemRarityID.Gray;
        }
    }
}