using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Extensions;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class AmarokItem : VanillaYoyoItem
    {
        public AmarokItem() : base(ItemID.Amarok) { }
    }

    public class AmarokProjectile : VanillaYoyoProjectile, IDrawPixelatedProjectile
    {
        public bool IsMainYoyo { get; private set; }

        private RingRenderer ringRenderer;

        public AmarokProjectile() : base(ProjectileID.Amarok) { }

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            IsMainYoyo = GetMainYoyoFlag(proj);

            proj.localAI[1] = -1;
        }

        public override void AI(Projectile proj)
        {
            if (proj.localAI[1] >= 0)
                proj.localAI[1] += 0.035f;

            if (Main.myPlayer != proj.owner || !Main.mouseRight || !Main.mouseRightRelease)
                return;

            proj.netUpdate = true;
            proj.localAI[1] = 0;
        }

        private bool GetMainYoyoFlag(Projectile proj)
        {
            var owner = Main.player[proj.owner];

            if (owner.OwnedProjectileCounts(proj.type) > 0)
                return false;

            // Fact that owned proj count was 0 does not guarantee that it is main yoyo
            // (In case of spawning 2+ yoyos at once)
            // Therefore, let's check other projs

            for (int i = 0; i < proj.whoAmI; i++)
            {
                ref var otherProjectile = ref Main.projectile[i];

                if (otherProjectile.active
                    && otherProjectile.owner == proj.owner
                    && otherProjectile.type == proj.type
                    && otherProjectile.TryGetGlobalProjectile(out AmarokProjectile otherGlobalProj)
                    && otherGlobalProj.IsMainYoyo)
                    return false;
            }

            return true;
        }

        void IPreDrawPixelatedProjectile.PreDrawPixelated(Projectile proj)
        {
            ref var progress = ref proj.localAI[1];

            if (progress < 0f || progress > 1f) return;

            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Amarok_Gradient", AssetRequestMode.ImmediateLoad);
            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;

            Main.spriteBatch.Draw(texture.Value, new Rectangle((int)position.X, (int)position.Y, Main.screenWidth * 2, (int)(texture.Height() * 0.2f * (1 - EasingFunctions.InExpo(progress)))), null, Color.White, EasingFunctions.OutQuad(progress) * MathHelper.Pi + MathHelper.PiOver4, texture.Size() * 0.5f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture.Value, new Rectangle((int)position.X, (int)position.Y, Main.screenWidth * 2, (int)(texture.Height() * 0.2f * (1 - EasingFunctions.InExpo(progress)))), null, Color.White, EasingFunctions.OutQuad(progress) * MathHelper.Pi + MathHelper.PiOver2 + MathHelper.PiOver4, texture.Size() * 0.5f, SpriteEffects.None, 0f);

            texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Amarok_Cross", AssetRequestMode.ImmediateLoad);

            Main.spriteBatch.Draw(texture.Value, position, null, Color.White, EasingFunctions.OutQuad(progress) * MathHelper.Pi, texture.Size() * 0.5f, EasingFunctions.OutQuad(progress) * 2f, SpriteEffects.None, 0f);
        }

        void IPostDrawPixelatedProjectile.PostDrawPixelated(Projectile proj)
        {
            ringRenderer ??= InitRingRenderer();

            ref var progress = ref proj.localAI[1];

            if (progress < 0f || progress > 1f) return;

            var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "DefaultPrimitive", AssetRequestMode.ImmediateLoad);
            var effect = effectAsset.Value;
            var effectParameters = effect.Parameters;

            effectParameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Amarok_Gradient", AssetRequestMode.ImmediateLoad).Value);
            effectParameters["TransformMatrix"].SetValue(ProjectileDrawLayers.PixelatedPrimitiveMatrices.TransformWithScreenOffset);

            var whiteColorVec4 = Color.White.ToVector4();

            effectParameters["ColorTL"].SetValue(whiteColorVec4);
            effectParameters["ColorTR"].SetValue(whiteColorVec4);
            effectParameters["ColorBL"].SetValue(whiteColorVec4);
            effectParameters["ColorBR"].SetValue(whiteColorVec4);

            ringRenderer.SetPosition(proj.Center + proj.gfxOffY * Vector2.UnitY)
                .SetThickness((1 - EasingFunctions.InExpo(progress)) * 16)
                .SetRadius(EasingFunctions.OutExpo(progress) * 16 * 5)
                .Draw(effect);
        }

        private RingRenderer InitRingRenderer()
            => new RingRenderer(16, 16, 16 * 10);
    }
}