using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SPYoyoMod.Common.PixelatedLayers;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Content.Dusts;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Mono.Cecil.Cil.OpCodes;

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

            // Remove onhit debuff
            IL_Projectile.StatusNPC += (il) =>
            {
                var c = new ILCursor(il);

                // if (type == 545 && Main.rand.Next(3) == 0)

                // IL_0736: ldarg.0
                // IL_0737: ldfld int32 Terraria.Projectile::'type'
                // IL_073c: ldc.i4 545
                // IL_0741: bne.un.s IL_076a

                ILLabel skipDebuffLabel = null;

                if (!c.TryGotoNext(MoveType.After,
                        i => i.MatchLdarg(0),
                        i => i.MatchLdfld<Projectile>("type"),
                        i => i.MatchLdcI4(ProjectileID.Cascade),
                        i => i.MatchBneUn(out skipDebuffLabel))) return;

                c.Emit(Ldc_I4_1);
                c.Emit(Brtrue, skipDebuffLabel);
            };
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

    public class CascadeProjectile : VanillaYoyoProjectile
    {
        private TrailRenderer trailRenderer;

        public CascadeProjectile() : base(ProjectileID.Cascade) { }

        public override void OnKill(Projectile proj, int timeLeft)
        {
            trailRenderer?.Dispose();
        }

        public override void AI(Projectile proj)
        {
            if (proj.velocity.Length() >= 3f && Main.rand.NextBool(4))
            {
                var dustIndex = Dust.NewDust(proj.position + Vector2.One * 4, proj.width - 2, proj.height - 2, ModContent.DustType<CircleGlowDust>(), 0f, 0f, 0, new Color(255, 135, 10));
                var dust = Main.dust[dustIndex];

                dust.velocity *= 0.4f;
            }

            trailRenderer?.SetNextPoint(proj.Center + proj.velocity);
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            DrawUtils.DrawYoyoString(proj, mountedCenter, (segmentCount, segmentIndex, position, rotation, height, color) =>
            {
                var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/FishingLine_WithShadow", AssetRequestMode.ImmediateLoad);
                var pos = position - Main.screenPosition;
                var rect = new Rectangle(0, 0, texture.Width(), (int)height);
                var origin = new Vector2(texture.Width() * 0.5f, 0f);
                var colour = Color.Lerp(Color.Transparent, new Color(255, 180, 95), EasingFunctions.InQuart(segmentIndex / (float)segmentCount) * 5f);

                Main.spriteBatch.Draw(texture.Value, pos, rect, colour, rotation, origin, 1f, SpriteEffects.None, 0f);
            });
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            trailRenderer ??= new TrailRenderer(20).SetWidth(f => MathHelper.Lerp(30f, 70f, f));

            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.UnderProjectiles, () =>
            {
                var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "CascadeTrail", AssetRequestMode.ImmediateLoad);
                var effect = effectAsset.Value;
                var effectParameters = effect.Parameters;

                var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Cascade_Trail", AssetRequestMode.ImmediateLoad);

                effectParameters["Texture0"].SetValue(texture.Value);
                effectParameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.TransformWithScreenOffset);
                effectParameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.025f);
                effectParameters["Color0"].SetValue(new Color(255, 255, 160).ToVector4());
                effectParameters["Color1"].SetValue(new Color(255, 80, 0).ToVector4());
                effectParameters["Color2"].SetValue(new Color(250, 50, 100).ToVector4());
                effectParameters["Color3"].SetValue(new Color(70, 30, 150).ToVector4());

                trailRenderer?.Draw(effect);
            });

            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Yoyo_GlowWithShadow", AssetRequestMode.ImmediateLoad);
            var color = new Color(255, 180, 95);

            Main.spriteBatch.Draw(texture.Value, position, null, color, proj.rotation, texture.Size() * 0.5f, proj.scale * 1.2f, SpriteEffects.None, 0f);

            return true;
        }

        /*void IPreDrawPixelatedProjectile.PreDrawPixelated(Projectile proj)
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

        void IDrawPrimitivesProjectile.PostDrawPrimitives(Terraria.Projectile proj, PrimitiveMatrices matrices)
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
        }

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

            Main.spriteBatch.Draw(texture.Value, position, null, color, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
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
        }*/
    }
}