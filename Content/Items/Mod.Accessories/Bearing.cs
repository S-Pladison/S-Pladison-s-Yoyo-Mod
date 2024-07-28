using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public sealed class BearingItem : ModItem
    {
        public override string Texture => $"{nameof(SPYoyoMod)}/Assets/Items/Mod.Accessories/Bearing_Item";

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 36;
            Item.height = 34;

            Item.rare = ItemRarityID.White;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 0, silver: 20, copper: 0);
        }
    }
}