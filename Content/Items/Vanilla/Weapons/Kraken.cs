using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class KrakenItem : VanillaYoyoItem
    {
        public override int YoyoType => ItemID.Kraken;
    }

    public class KrakenProjectile : VanillaYoyoProjectile
    {
        public override int YoyoType => ProjectileID.Kraken;

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            var rotOffset = Main.rand.NextFloat(Main.rand.NextFloat(MathHelper.TwoPi));

            for (int i = 0; i < 7; i++)
            {
                Projectile.NewProjectile(proj.GetSource_OnHit(target), proj.Center, Vector2.UnitX.RotatedBy(MathHelper.TwoPi / 7 * i + rotOffset), ModContent.ProjectileType<KrakenTentacleProjectile>(), proj.damage, proj.knockBack, proj.owner);
            }
        }
    }

    public class KrakenTentacleProjectile : ModProjectile
    {
        public override string Texture => ModAssets.MiscPath + "Invisible";

        private bool initialized;
        private List<Vector2> points;
        private LineRenderer lineRenderer;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = 2;
            Projectile.height = 2;

            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 300;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
        }

        private float Beta;
        private float Omega;

        public override void OnKill(int timeLeft)
        {
            lineRenderer?.Dispose();
        }

        public override void AI()
        {
            if (!initialized)
            {
                points = new List<Vector2>();
                initialized = true;
            }

            if (points.Count >= 15)
            {
                Projectile.velocity = Vector2.Zero;
                return;
            }

            Beta += Main.rand.NextFloat(-0.002f, 0.002f);
            Omega += Beta;

            if (Math.Abs(Omega) > 0.3f)
            {
                Omega *= 0.95f;
            }
            if (Math.Abs(Beta) > 0.05f)
            {
                Beta *= 0.95f;
            }

            Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 13f * ((15 - points.Count) / 15f);
            //Projectile.velocity = Projectile.velocity.RotatedBy(Main.rand.NextFloat(-0.35f, 0.35f));
            Projectile.velocity = Projectile.velocity.RotatedBy(Omega);

            if (Projectile.timeLeft % 4 == 0)
                points.Add(Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            /*for (int i = 1; i < points.Count; i++)
            {
                //Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)(points[i].X - Main.screenPosition.X) - 1, (int)(points[i].Y - Main.screenPosition.Y) - 1, 2, 2), Color.White);

                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, points[i - 1] - Main.screenPosition, new Rectangle(0, 0, (int)(points[i] - points[i - 1]).Length(), 1), Color.White, (points[i] - points[i - 1]).ToRotation(), new Vector2(0f, 0.5f), 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            }*/

            var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "DefaultStrip", AssetRequestMode.ImmediateLoad);
            var effect = effectAsset.Value;
            var effectParameters = effect.Parameters;

            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Tessst", AssetRequestMode.ImmediateLoad);

            effectParameters["Texture0"].SetValue(texture.Value);
            effectParameters["TransformMatrix"].SetValue(PrimitiveMatrices.DefaultPrimitiveMatrices.TransformWithScreenOffset);

            var blackColorVec4 = Color.White.ToVector4();

            effectParameters["ColorTL"].SetValue(blackColorVec4);
            effectParameters["ColorTR"].SetValue(blackColorVec4);
            effectParameters["ColorBL"].SetValue(blackColorVec4);
            effectParameters["ColorBR"].SetValue(blackColorVec4);

            lineRenderer ??= new LineRenderer(24f, false);
            lineRenderer.SetPoints(points).Draw(effect);

            return false;
        }
    }
}