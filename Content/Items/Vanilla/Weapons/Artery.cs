using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Graphics.PixelatedLayers;
using SPYoyoMod.Common.Graphics.Renderers;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    // https://www.artstation.com/artwork/zPLbPZ
    [Autoload(false)]
    public class ArteryItem : VanillaYoyoItem
    {
        public override int YoyoType => ItemID.CrimsonYoyo;
    }

    [Autoload(false)]
    public class ArteryProjectile : VanillaYoyoProjectile
    {
        private bool initialized;
        private QuadRenderer auraQuadRenderer;

        public override int YoyoType => ProjectileID.CrimsonYoyo;

        public void OnInitialize()
        {
            if (Main.dedServ) return;

            auraQuadRenderer = new QuadRenderer();
        }

        public override void AI(Projectile proj)
        {
            if (!initialized)
            {
                OnInitialize();

                initialized = true;
            }
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.UnderProjectiles, () => DrawPixelatedBlackAura(proj));
            return true;
        }

        public void DrawPixelatedBlackAura(Projectile proj)
        {
            var effect = ModContent.Request<Effect>(ModAssets.EffectsPath + "ArteryBlackAura", AssetRequestMode.ImmediateLoad).Prepare(parameters =>
            {
                parameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.MiscPath + "Test", AssetRequestMode.ImmediateLoad).Value);
                parameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.Transform);
                parameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.007f);
            });

            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var rectangle = new Rectangle((int)position.X - 35, (int)position.Y - 35, 70, 70);

            auraQuadRenderer?.SetPoints(rectangle).Draw(effect);

            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Circle_BlackToAlpha_PremultipliedAlpha", AssetRequestMode.ImmediateLoad);
            var color = Color.Black;

            Main.spriteBatch.Draw(texture.Value, position, null, color, 0f, texture.Size() * 0.5f, 0.2f, SpriteEffects.None, 0f);
        }
    }
}