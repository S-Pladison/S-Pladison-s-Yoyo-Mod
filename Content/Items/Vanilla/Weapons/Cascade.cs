using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Extensions;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class CascadeItem : VanillaYoyoItem
    {
        public CascadeItem() : base(ItemID.Cascade) { }

        public override void YoyoSetStaticDefaults()
        {
            // Because this yoyo cannot be obtained in hardmode (since Hel-Fire drops instead)
            // * Rework required when tModLoader update shimmer system
            ModEvents.OnHardmodeStart += AddShimmerTransforms;
            ModEvents.OnWorldLoad += () => { if (Main.hardMode) AddShimmerTransforms(); };
            ModEvents.OnWorldUnload += RemoveShimmerTransforms;
        }

        private static void AddShimmerTransforms()
        {
            ItemID.Sets.ShimmerTransformToItem[ItemID.Cascade] = ItemID.HelFire;
            ItemID.Sets.ShimmerTransformToItem[ItemID.HelFire] = ItemID.Cascade;
        }

        private static void RemoveShimmerTransforms()
        {
            ItemID.Sets.ShimmerTransformToItem[ItemID.Cascade] = -1;
            ItemID.Sets.ShimmerTransformToItem[ItemID.HelFire] = -1;
        }
    }

    public class CascadeProjectile : VanillaYoyoProjectile, IPreDrawPixelatedProjectile, IPostDrawAdditiveProjectile, IDrawDistortionProjectile
    {
        public override bool InstancePerEntity { get => true; }
        public bool IsMainYoyo { get; private set; }

        private RingRenderer ringRenderer;

        public CascadeProjectile() : base(ProjectileID.Cascade) { }

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            IsMainYoyo = GetMainYoyoFlag(proj);
        }

        public override void OnKill(Projectile proj, int timeLeft)
        {
            ringRenderer?.Dispose();
        }

        public override void AI(Projectile proj)
        {
            if (!IsMainYoyo) return;

            if (proj.localAI[1] == 0f)
            {
                for (int i = 0; i < 12; i++)
                {
                    Dust.NewDustPerfect(proj.Center, ModContent.DustType<CascadeDust>(), Vector2.Zero);
                }

                DustUtils.SpawnDustCircle(proj.Center, 16f * 2f, 12, _ => ModContent.DustType<CascadeDust>(), (dust, index, angle) =>
                {
                    dust.velocity += Vector2.UnitX.RotatedBy(angle) * Main.rand.NextFloat(1f, 2f);
                });

                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode);
            }

            if (proj.localAI[1] >= 0f)
                proj.localAI[1] += 0.05f;

            if (proj.localAI[1] >= 4f)
                proj.localAI[1] = 0f;
        }

        void IPreDrawPixelatedProjectile.PreDrawPixelated(Projectile proj)
        {
            if (!IsMainYoyo) return;

            ref var progress = ref proj.localAI[1];

            var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "CascadeRing", AssetRequestMode.ImmediateLoad);
            var effect = effectAsset.Value;
            var effectParameters = effect.Parameters;

            effectParameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Cascade_Ring", AssetRequestMode.ImmediateLoad).Value);
            effectParameters["TransformMatrix"].SetValue(ProjectileDrawLayers.PixelatedPrimitiveMatrices.TransformWithScreenOffset);
            effectParameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.025f);

            ringRenderer ??= new RingRenderer(20, 16f * 3f, 16f * 3f);
            ringRenderer
                .SetThickness(MathHelper.Clamp(1f - progress, 0f, 1f) * 64f)
                .SetRadius(EasingFunctions.OutExpo(progress) * 16f * 3f)
                .SetPosition(proj.Center + proj.gfxOffY * Vector2.UnitY)
                .Draw(effect);
        }

        void IPostDrawAdditiveProjectile.PostDrawAdditive(Projectile proj)
        {
            ref var progress = ref proj.localAI[1];

            var drawPosition = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Circle", AssetRequestMode.ImmediateLoad);
            var color = Color.Orange * (1f - EasingFunctions.InExpo(progress)) * 0.35f;
            var scale = proj.scale * 0.55f;

            Main.spriteBatch.Draw(texture.Value, drawPosition, null, color, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/LightBeam", AssetRequestMode.ImmediateLoad);
            color = Color.Orange;
            scale = proj.scale;

            Main.spriteBatch.Draw(texture.Value, drawPosition, null, color, 0f, new Vector2(0f, texture.Height() * 0.5f), scale, SpriteEffects.None, 0f);
        }

        /*void IDrawPrimitivesProjectile.PostDrawPrimitives(Terraria.Projectile proj, PrimitiveMatrices matrices)
        {
            if (!IsMainYoyo) return;

            ref var progress = ref proj.localAI[1];

            var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "CascadeRing", AssetRequestMode.ImmediateLoad);
            var effect = effectAsset.Value;
            var effectParameters = effect.Parameters;

            effectParameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Cascade_Ring", AssetRequestMode.ImmediateLoad).Value);
            effectParameters["TransformMatrix"].SetValue(matrices.TransformWithScreenOffset);
            effectParameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.025f);

            ringRenderer ??= new RingRenderer(20, 16f * 3f, 16f * 3f);
            ringRenderer
                .SetThickness(MathHelper.Clamp(1f - progress, 0f, 1f) * 64f)
                .SetRadius(EasingFunctions.OutExpo(progress) * 16f * 3f)
                .SetPosition(proj.Center + proj.gfxOffY * Vector2.UnitY)
                .Draw(effect);
        }*/

        void IDrawDistortionProjectile.DrawDistortion(Projectile proj)
        {
            if (!IsMainYoyo) return;

            /*ref var progress = ref proj.localAI[1];

            var builder = new EasingBuilder(
                (EasingFunctions.InOutCubic, 0.35f, 0f, 1f),
                (EasingFunctions.InCirc, 0.35f, 1f, 0f)
            );

            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/RingNormalMap", AssetRequestMode.ImmediateLoad);
            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var scale = MathHelper.Clamp(progress, 0, 1) * 0.3f;
            var color = Color.White.MultiplyB(builder.Evaluate(progress));

            Main.spriteBatch.Draw(texture.Value, position, null, color, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);*/
        }

        private static bool GetMainYoyoFlag(Projectile proj)
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
                    && otherProjectile.TryGetGlobalProjectile(out CascadeProjectile otherGlobalProj)
                    && otherGlobalProj.IsMainYoyo)
                    return false;
            }

            return true;
        }
    }

    public class CascadeDust : ModDust
    {
        public override string Texture { get => ModAssets.DustsPath + "Cascade"; }

        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.scale *= Main.rand.NextFloat(1f, 1.2f);
            dust.color = Color.White;
        }

        public override bool PreDraw(Dust dust)
        {
            /*var scale = new Vector2(dust.scale * MathHelper.Max(1f, dust.velocity.Length() * 2f), dust.scale);
            Main.spriteBatch.Draw(Texture2D.Value, dust.position - Main.screenPosition, null, dust.color, dust.velocity.ToRotation(), new Vector2(16, 16), scale, SpriteEffects.None, 0);*/
            return false;
        }
    }
}