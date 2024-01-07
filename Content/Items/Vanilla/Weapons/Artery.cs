using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class ArteryYoyoItem : VanillaYoyoItem
    {
        public ArteryYoyoItem() : base(yoyoType: ItemID.CrimsonYoyo) { }
    }

    public class ArteryYoyoProjectile : VanillaYoyoProjectile, IPostDrawPixelatedProjectile
    {
        public ArteryYoyoProjectile() : base(yoyoType: ProjectileID.CrimsonYoyo) { }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            DrawUtils.DrawYoyoString(proj, mountedCenter, (index, position, rotation, height, color) =>
            {
                //Main.spriteBatch.Draw(TextureAssets.FishingLine.Value, position - Main.screenPosition, new Rectangle(0, 0, TextureAssets.FishingLine.Width(), (int)height), Color.Red, rotation, new Vector2(TextureAssets.FishingLine.Width() * 0.5f, 0f), 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            });
        }

        public void PostDrawPixelated(Projectile proj)
        {
            Main.spriteBatch.Draw(TextureAssets.Sun2.Value, proj.Center - Main.screenPosition, null, Color.White, MathHelper.PiOver4, TextureAssets.Sun2.Size() * 0.5f, 1f, SpriteEffects.None, 0);
        }
    }
}