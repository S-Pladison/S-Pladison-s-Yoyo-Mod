using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace SPYoyoMod.Common
{
    public class ItemInventoryDrawHooks : ILoadable
    {
        void ILoadable.Load(Mod mod)
        {
            On_ItemSlot.DrawItemIcon += (orig, item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor) =>
            {
                // I don't know why, but hook On_ItemSlot.DrawItem_GetColorAndScale doesn't get called without this dummy placeholder
                return orig(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
            };

            On_ItemSlot.DrawItem_GetColorAndScale += (On_ItemSlot.orig_DrawItem_GetColorAndScale orig, Item item, float s, ref Color cW, float sL, ref Rectangle f, out Color iL, out float finalDrawScale) =>
            {
                orig(item, s, ref cW, sL, ref f, out iL, out finalDrawScale);

                var scaleMult = ModSets.Items.InventoryDrawScaleMultiplier[item.type];

                if (scaleMult.HasValue)
                {
                    finalDrawScale *= scaleMult.Value;
                }
            };
        }

        void ILoadable.Unload() { }
    }
}