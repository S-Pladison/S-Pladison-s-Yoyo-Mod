using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using SPYoyoMod.Utils.Extensions;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class BlackholeItem : YoyoItem
    {
        public override string Texture { get => ModAssets.ItemsPath + "Blackhole"; }

        public BlackholeItem() : base(gamepadExtraRange: 15) { }

        public override void YoyoSetStaticDefaults()
        {
            ModSets.Items.InventoryDrawScaleMultiplier[Type] = 1.3f;
        }

        public override void YoyoSetDefaults()
        {
            Item.damage = 43;
            Item.knockBack = 2.5f;

            Item.shoot = ModContent.ProjectileType<BlackholeProjectile>();

            Item.rare = ItemRarityID.Lime;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    public class BlackholeProjectile : YoyoProjectile, IDrawPixelatedProjectile, IDrawDistortionProjectile
    {
        public static readonly float GravityRadius;

        static BlackholeProjectile()
        {
            GravityRadius = 16 * 10;
        }

        public override string Texture { get => ModAssets.ProjectilesPath + "Blackhole"; }
        public float RadiusProgress { get => Projectile.localAI[1]; set => Projectile.localAI[1] = value; }
        public float TimeForVisualEffects { get => (Projectile.whoAmI * 200f + (float)Main.timeForVisualEffects) % 216000f; }

        public BlackholeProjectile() : base(lifeTime: -1f, maxRange: 300f, topSpeed: 13f) { }

        public override void YoyoOnSpawn(Player owner, IEntitySource source)
        {
            if (!IsMainYoyo) return;

            ModContent.GetInstance<BlackholeRenderTargetContent>().AddProjectile(this);
        }

        public override void AI()
        {
            if (!IsMainYoyo) return;

            Lighting.AddLight(Projectile.Center, new Color(171, 97, 255).ToVector3() * 0.6f);

            if (IsReturning) return;

            RadiusProgress += !IsReturning ? 0.05f : -0.1f;
            RadiusProgress = Math.Clamp(RadiusProgress, 0, 1);

            var currentRadius = GravityRadius * EaseFunctions.InOutSine(RadiusProgress);
            var targets = NPCUtils.NearestNPCs(
                center: Projectile.Center,
                radius: currentRadius,
                predicate: (npc) =>
                    npc.CanBeChasedBy(Projectile, false) &&
                    !npc.boss &&
                    !NPCID.Sets.ShouldBeCountedAsBoss[npc.type] &&
                    Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height)
            );

            foreach (var (npc, distance) in targets)
            {
                var vector = Projectile.Center - npc.Center;
                var progress = 1 - distance / currentRadius;

                vector *= 15f / distance;

                npc.velocity = Vector2.Lerp(vector * progress, npc.velocity, 0.8f);
                npc.netUpdate = true;
            }
        }

        public override void YoyoOnHitNPC(Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!IsMainYoyo || IsReturning) return;

            OnHitParticles();
        }

        public override void YoyoOnHitPlayer(Player owner, Player target, Player.HurtInfo info)
        {
            if (!IsMainYoyo || IsReturning) return;

            OnHitParticles();
        }

        public void OnHitParticles()
        {
            var blackholeRTContent = ModContent.GetInstance<BlackholeRenderTargetContent>();

            for (int i = 0; i < 7; i++)
            {
                var position = Projectile.Center + Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Main.rand.NextFloat(20);
                var velocity = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Main.rand.NextFloat(0.75f);
                var particle = new BlackholeParticle(position, velocity);

                blackholeRTContent.AddParticle(particle);
            }
        }

        public override void PostDrawYoyoString(Vector2 mountedCenter)
        {
            if (!IsMainYoyo) return;

            DrawUtils.DrawYoyoString(Projectile, mountedCenter, (segmentCount, segmentIndex, position, rotation, height, color) =>
            {
                var pos = position - Main.screenPosition;
                var rect = new Rectangle(0, 0, TextureAssets.FishingLine.Width(), (int)height);
                var origin = new Vector2(TextureAssets.FishingLine.Width() * 0.5f, 0f);
                var colour = Color.Lerp(Color.Transparent, new Color(230, 135, 243), EaseFunctions.InQuart(segmentIndex / (float)segmentCount) * 2f);

                Main.spriteBatch.Draw(TextureAssets.FishingLine.Value, pos, rect, colour, rotation, origin, 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            });
        }

        public void DrawSpaceMask()
        {
            var scale = Projectile.scale * EaseFunctions.InOutSine(RadiusProgress);
            var drawPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Circle", AssetRequestMode.ImmediateLoad);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, 0f, texture.Size() * 0.5f, 0.64f * scale, SpriteEffects.None, 0f);

            texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Smoke", AssetRequestMode.ImmediateLoad);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, TimeForVisualEffects * 0.02f, texture.Size() * 0.5f, 0.52f * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, TimeForVisualEffects * 0.01f, texture.Size() * 0.5f, 0.47f * scale, SpriteEffects.FlipHorizontally, 0f);

            texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Spiral", AssetRequestMode.ImmediateLoad);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, TimeForVisualEffects * 0.02f, texture.Size() * 0.5f, 0.32f * scale, SpriteEffects.None, 0f);
        }

        void IDrawPixelatedProjectile.PostDrawPixelated(Projectile _)
        {
            if (!IsMainYoyo) return;

            var drawPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Blackhole_LensFlare", AssetRequestMode.ImmediateLoad);
            var rotation = MathF.Sin(TimeForVisualEffects * 0.01f);
            var scale = Projectile.scale;
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, rotation, texture.Size() * 0.5f, 0.85f * scale, SpriteEffects.None, 0f);
        }

        void IDrawDistortionProjectile.DrawDistortion(Projectile proj)
        {
            if (!IsMainYoyo) return;

            var drawPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/DistortedRing", AssetRequestMode.ImmediateLoad);
            var rotation = TimeForVisualEffects * 0.02f;
            var scale = Projectile.scale * EaseFunctions.InOutSine(RadiusProgress);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.Gray, rotation, texture.Size() * 0.5f, 0.6f * scale, SpriteEffects.None, 0f);
        }
    }

    public class BlackholeParticle : IParticle
    {
        public bool ShouldBeRemovedFromRenderer { get => timeLeft <= 0; }
        public float Progress { get => MathHelper.Lerp(1f, 0f, timeLeft / (float)initTimeLeft); }

        private readonly Asset<Texture2D> texture;
        private readonly Vector2 origin;
        private readonly Vector2 initVelocity;
        private readonly int initTimeLeft;
        private readonly float rotation;

        private Vector2 position;
        private Vector2 velocity;
        private int timeLeft;

        public BlackholeParticle(Vector2 position, Vector2 velocity)
        {
            texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Smoke", AssetRequestMode.ImmediateLoad);
            origin = texture.Size() * 0.5f;
            timeLeft = initTimeLeft = Main.rand.Next(60 * 1, 60 * 2);
            rotation = Main.rand.NextFloat(MathHelper.TwoPi);

            this.position = position;
            this.velocity = initVelocity = velocity;
        }

        public void Update(ref ParticleRendererSettings settings)
        {
            velocity = Vector2.Lerp(initVelocity, Vector2.Zero, Progress);
            position += velocity;
            timeLeft--;

            var easeResult = EaseFunctions.InOutCirc(Progress);
            var lightColorMult = 0.4f * MathF.Min(easeResult * (1f - easeResult) * 20f, 1f);
            Lighting.AddLight(position, new Color(171, 97, 255).ToVector3() * lightColorMult);
        }

        public void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
        {
            var easeResult = EaseFunctions.InOutCirc(Progress);
            var color = Color.White * easeResult * (1f - easeResult) * 20f;
            spriteBatch.Draw(texture.Value, settings.AnchorPosition + position, null, color, rotation, origin, 0.35f, SpriteEffects.None, 0f);
        }
    }

    public class BlackholeRenderTargetContent : RenderTargetContent
    {
        public override Point Size { get => new(Main.screenWidth / 2, Main.screenHeight / 2); }

        private Asset<Effect> effect;
        private ParticleRenderer particleRenderer;
        private List<int> projectiles;

        public void AddParticle(BlackholeParticle particle)
        {
            if (particle is null) return;

            particleRenderer.Add(particle);
        }

        public void AddProjectile(BlackholeProjectile modProjectile)
        {
            if (modProjectile is null) return;

            projectiles.Add(modProjectile.Projectile.whoAmI);
        }

        public override void Load()
        {
            projectiles = new();
            particleRenderer = new();

            ModEvents.OnPostUpdateEverything += () => particleRenderer.Update();
            ModEvents.OnWorldUnload += () => particleRenderer.Clear();

            On_Main.DoDraw_WallsAndBlacks += (orig, main) =>
            {
                orig(main);

                if (IsRenderedInThisFrame && TryGetRenderTarget(out _))
                {
                    Main.spriteBatch.End(out SpriteBatchSnapshot spriteBatchSnapshot);
                    DrawToScreen();
                    Main.spriteBatch.Begin(spriteBatchSnapshot);
                }
            };
        }

        public override bool PreRender()
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                ref var proj = ref Main.projectile[projectiles[i]];

                if (proj is null || !proj.active || proj.ModProjectile is not BlackholeProjectile)
                {
                    projectiles.RemoveAt(i);
                    i--;
                }
            }

            return particleRenderer.Particles.Count > 0 || projectiles.Count > 0;
        }

        public override void DrawToTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix);

            particleRenderer.Settings.AnchorPosition = -Main.screenPosition;
            particleRenderer.Draw(Main.spriteBatch);

            for (int i = 0; i < projectiles.Count; i++)
            {
                ref var proj = ref Main.projectile[projectiles[i]];

                (proj.ModProjectile as BlackholeProjectile).DrawSpaceMask();
            }

            Main.spriteBatch.End();
        }

        private void DrawToScreen()
        {
            effect ??= LoadEffect();

            var parameters = effect.Value.Parameters;
            parameters["Texture0Size"].SetValue(renderTarget.Size());
            parameters["EffectMatrix"].SetValue(Main.GameViewMatrix.EffectMatrix);
            parameters["ScreenPosition"].SetValue(Main.screenPosition + Main.ScreenSize.ToVector2() * 0.5f);
            parameters["Time"].SetValue((float)Main.timeForVisualEffects * 0.1f);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, effect.Value, Main.GameViewMatrix.ZoomMatrix);
            Main.spriteBatch.Draw(renderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }

        private Asset<Effect> LoadEffect()
        {
            var spaceTexture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Space", AssetRequestMode.ImmediateLoad);
            var cloudsTexture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Clouds", AssetRequestMode.ImmediateLoad);
            var effect = ModContent.Request<Effect>(ModAssets.EffectsPath + "BlackholeBackground", AssetRequestMode.ImmediateLoad);

            var parameters = effect.Value.Parameters;
            parameters["Texture1"].SetValue(spaceTexture.Value);
            parameters["Texture1Size"].SetValue(spaceTexture.Size());
            parameters["Texture2"].SetValue(cloudsTexture.Value);
            parameters["Texture2Size"].SetValue(cloudsTexture.Size());
            parameters["Cloud1Color"].SetValue(new Color(198, 50, 189).ToVector4());
            parameters["Cloud2Color"].SetValue(new Color(25, 25, 76).ToVector4());

            return effect;
        }
    }
}