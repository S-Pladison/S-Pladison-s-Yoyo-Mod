using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.Interfaces;
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

    public class ArteryYoyoProjectile : VanillaYoyoProjectile, IDrawDistortionProjectile
    {
        public ArteryYoyoProjectile() : base(yoyoType: ProjectileID.CrimsonYoyo) { }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            DrawUtils.DrawYoyoString(proj, mountedCenter, (index, position, rotation, height, color) =>
            {
                //Main.spriteBatch.Draw(TextureAssets.FishingLine.Value, position - Main.screenPosition, new Rectangle(0, 0, TextureAssets.FishingLine.Width(), (int)height), Color.Red, rotation, new Vector2(TextureAssets.FishingLine.Width() * 0.5f, 0f), 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            });
        }

        public void DrawDistortion()
        {
            // ...
        }
    }
}