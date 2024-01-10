using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Utils.DataStructures;
using SPYoyoMod.Utils.Extensions;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class ArteryItem : VanillaYoyoItem
    {
        public ArteryItem() : base(yoyoType: ItemID.CrimsonYoyo) { }
    }

    public class ArteryProjectile : VanillaYoyoProjectile
    {
        //private static LineRenderer lineRenderer;

        public ArteryProjectile() : base(yoyoType: ProjectileID.CrimsonYoyo) { }

        public override void Load()
        {
            //Main.QueueMainThreadAction(() => lineRenderer = new LineRenderer(4, true).SetColor(_ => Color.Cyan));
            //Main.QueueMainThreadAction(() => lineRenderer = new LineRenderer(4));
        }

        public override void Unload()
        {
            //lineRenderer = null;
        }

        public override void PostDraw(Projectile projectile, Color lightColor)
        {
            Main.spriteBatch.End(out SpriteBatchSnapshot spriteBatchSnapshot);

            var matrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);

            var TransformMatrix = matrix;
            TransformMatrix *= Main.GameViewMatrix.EffectMatrix;

            matrix = Matrix.CreateTranslation(Main.screenWidth / 2, Main.screenHeight / -2, 0);
            matrix *= Matrix.CreateRotationZ(MathHelper.Pi);

            TransformMatrix *= matrix;
            TransformMatrix *= Matrix.CreateScale(Main.GameViewMatrix.Zoom.X, Main.GameViewMatrix.Zoom.Y, 1f);

            matrix = Matrix.CreateOrthographic(Main.screenWidth, Main.screenHeight, 0, 1000);

            TransformMatrix *= matrix;

            var effect = ModContent.Request<Effect>(ModAssets.EffectsPath + "DefaultPrimitive", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            effect.Value.Parameters["Texture0"].SetValue(TextureAssets.MagicPixel.Value);
            effect.Value.Parameters["WorldViewProj"].SetValue(TransformMatrix);

            //var points = new[] { Main.LocalPlayer.position - Main.screenPosition, Main.MouseWorld - Main.screenPosition, Main.LocalPlayer.position + Vector2.One * 100 - Main.screenPosition, Main.MouseWorld - Vector2.One * 100f - Main.screenPosition };
            var points = new[] { Main.LocalPlayer.Center, projectile.Center, projectile.Center - new Vector2(50, 50), Main.LocalPlayer.Center - new Vector2(50, -50) };

            for (int i = 0; i < points.Length; i++)
            {
                points[i] -= Main.screenPosition;
            }

            //lineRenderer.SetPoints(points);
            //lineRenderer.Draw(effect);

            Main.spriteBatch.Begin(spriteBatchSnapshot);
        }
    }
}