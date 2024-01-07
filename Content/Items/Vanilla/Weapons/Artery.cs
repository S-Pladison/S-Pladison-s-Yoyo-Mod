using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
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
            DrawUtils.DrawYoyoString(proj, mountedCenter, (index, position, rotation, height, color) =>
            {
                //Main.spriteBatch.Draw(TextureAssets.FishingLine.Value, position - Main.screenPosition, new Rectangle(0, 0, TextureAssets.FishingLine.Width(), (int)height), Color.Red, rotation, new Vector2(TextureAssets.FishingLine.Width() * 0.5f, 0f), 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            });
        }
    }

    public class ArteryYoyoRenderTargetContent : ScreenRenderTargetContent
    {
        public override bool Active { get => true; }

        protected override void OnLoad()
        {
            On_Main.DrawProjectiles += (orig, main) =>
            {
                /*if (TryGetRenderTarget(out RenderTarget2D target))
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
                    Main.spriteBatch.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                }*/

                orig(main);
            };
        }

        public override void DrawToTarget()
        {
            /*Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(TextureAssets.Sun2.Value, Main.LocalPlayer.Center - Main.screenPosition, Color.White);
            Main.spriteBatch.End();*/
        }
    }
}