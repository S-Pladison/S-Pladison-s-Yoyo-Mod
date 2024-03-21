using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.Graphics.PixelatedLayers;
using SPYoyoMod.Utils;
using System;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Dusts
{
    public class SmokeDust : ModDust<SmokeDust.CustomData>
    {
        public class CustomData
        {
            public Color ColorStart;
            public bool ColorStartGlow;
            public Color ColorEnd;
            public bool ColorEndGlow;

            public CustomData(Color colorStart, bool colorStartGlow, Color colorEnd, bool colorEndGlow)
            {
                ColorStart = colorStart;
                ColorStartGlow = colorStartGlow;
                ColorEnd = colorEnd;
                ColorEndGlow = colorEndGlow;
            }
        }

        public override string Texture => ModAssets.MiscPath + "Smoke_BlackToAlpha_PremultipliedAlpha";

        public override void OnSpawn(Dust dust)
        {
            dust.frame = new Rectangle(0, 0, 256, 256);
            dust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override bool Update(Dust dust, CustomData customData)
        {
            dust.position += dust.velocity;
            dust.velocity *= 0.94f;
            dust.velocity.Y -= 0.02f;
            dust.alpha += 5;
            dust.scale += 0.005f;
            dust.rotation += MathF.Sign(dust.velocity.Length()) * 0.05f;

            if (dust.alpha >= 255) dust.active = false;

            return false;
        }

        public override bool PreDraw(Dust dust, CustomData customData)
        {
            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.OverSolidTiles, () =>
            {
                var texture = ModContent.Request<Texture2D>(Texture).Value;
                var startColor = !customData.ColorStartGlow ? Lighting.GetColor(dust.position.ToTileCoordinates(), customData.ColorStart) : customData.ColorStart;
                var endColor = !customData.ColorEndGlow ? Lighting.GetColor(dust.position.ToTileCoordinates(), customData.ColorEnd) : customData.ColorEnd;
                var colorProgress = EasingFunctions.InOutQuint(dust.alpha / 255f);
                var alphaProgress = 1f - EasingFunctions.InQuint(dust.alpha / 255f);
                var color = DataStructureUtils.Multiply(Color.Lerp(startColor, endColor, colorProgress), dust.color) * alphaProgress;

                Main.spriteBatch.Draw(texture, dust.position - Main.screenPosition, dust.frame, color, dust.rotation, texture.Size() / 2f, dust.scale, 0f, 0f);
            });

            return false;
        }
    }
}
