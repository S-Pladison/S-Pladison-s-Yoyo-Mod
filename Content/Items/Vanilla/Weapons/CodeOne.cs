using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Interfaces;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class CodeOneItem : VanillaYoyoItem
    {
        public CodeOneItem() : base(ItemID.Code1) { }
    }

    public class CodeOneProjectile : VanillaYoyoProjectile, IPreDrawAdditiveProjectile
    {
        // https://www.artstation.com/artwork/vD8AvD

        public CodeOneProjectile() : base(ProjectileID.Code1) { }

        private ParticleRenderer blueParticleRenderer;
        private ParticleRenderer blackParticleRenderer;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            base.OnSpawn(projectile, source);
        }

        void IPreDrawAdditiveProjectile.PreDrawAdditive(Projectile proj)
        {
            // Рисуем голубые молнии и голубое свечение
        }

        public override void PostDraw(Projectile proj, Color lightColor)
        {
            // Рисуем синие дуги (круги)
            // Рисуем черные фигни

            var drawPosition = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/CodeOne_Twirl", AssetRequestMode.ImmediateLoad);
            var scale = proj.scale;

            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, -(float)Main.timeForVisualEffects * 0.1f, texture.Size() * 0.5f, 0.2f * scale, SpriteEffects.None, 0f);
        }
    }
}