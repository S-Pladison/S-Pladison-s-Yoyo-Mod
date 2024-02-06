using Microsoft.Xna.Framework;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class ChikItem : VanillaYoyoItem
    {
        public ChikItem() : base(ItemID.Chik) { }
    }

    public class ChikProjectile : VanillaYoyoProjectile
    {
        public ChikProjectile() : base(ProjectileID.Chik) { }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            DrawUtils.DrawYoyoString(proj, mountedCenter, (segmentCount, segmentIndex, position, rotation, height, color) =>
            {
                var pos = position - Main.screenPosition;
                var rect = new Rectangle(0, 0, TextureAssets.FishingLine.Width(), (int)height);
                var origin = new Vector2(TextureAssets.FishingLine.Width() * 0.5f, 0f);
                var colour = Color.Lerp(Color.Transparent, new Color(247, 77, 222), EasingFunctions.InQuart(segmentIndex / (float)segmentCount) * 2f);

                Main.spriteBatch.Draw(TextureAssets.FishingLine.Value, pos, rect, colour, rotation, origin, 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            });
        }
    }
}