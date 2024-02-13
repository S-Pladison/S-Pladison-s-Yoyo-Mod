using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.PixelatedLayers;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Dusts
{
    public class CircleGlowDust : ModDust
    {
        public override string Texture => ModAssets.DustsPath + "CircleGlow";

        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.frame = new Rectangle(0, 0, 32, 32);
        }

        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            return dust.color;
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;
            dust.velocity *= 0.96f;
            dust.scale *= 0.95f;
            dust.alpha += 5;

            if (dust.alpha > 255 || dust.scale <= 0) dust.active = false;

            return false;
        }

        public override bool PreDraw(Dust dust)
        {
            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.OverDusts, () =>
            {
                var position = dust.position - Main.screenPosition;
                var colorProgress = 1f - dust.alpha / 255f;

                var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Circle_BlackToAlpha_PremultipliedAlpha", AssetRequestMode.ImmediateLoad);
                var origin = texture.Size() * 0.5f;

                Main.spriteBatch.Draw(texture.Value, position, null, Color.Black * 0.5f * colorProgress, dust.rotation, origin, dust.scale * 0.12f, SpriteEffects.None, 0f);

                texture = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad);
                origin = texture.Size() * 0.5f;

                Main.spriteBatch.Draw(texture.Value, position, dust.frame, dust.color with { A = 0 } * colorProgress, dust.rotation, origin, dust.scale * 1f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture.Value, position, dust.frame, Color.White with { A = 0 } * colorProgress, dust.rotation, origin, dust.scale * 0.33f, SpriteEffects.None, 0f);
            });

            return false;
        }
    }
}