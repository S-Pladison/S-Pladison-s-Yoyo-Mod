using Microsoft.Xna.Framework;
using SPYoyoMod.Common;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla
{
    public class ArteryYoyoItem : VanillaYoyoItem
    {
        public ArteryYoyoItem() : base(yoyoType: ItemID.CrimsonYoyo) { }
    }

    public class ArteryYoyoProjectile : VanillaYoyoProjectile
    {
        public ArteryYoyoProjectile() : base(yoyoType: ProjectileID.CrimsonYoyo) { }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            //Main.spriteBatch.Draw(TextureAssets.Sun.Value, proj.Center - Main.screenPosition, Color.White);

            DrawUtils.DrawYoyoString(proj, mountedCenter, (index, position, rotation, height, color) =>
            {
                Main.spriteBatch.Draw(TextureAssets.FishingLine.Value, position - Main.screenPosition, new Rectangle(0, 0, TextureAssets.FishingLine.Width(), (int)height), Color.Red, rotation, Vector2.Zero, 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                //Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, position - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color.Red, rotation, Vector2.Zero, 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            });
        }
    }
}