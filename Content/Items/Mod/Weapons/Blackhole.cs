using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using SPYoyoMod.Utils.Extensions;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
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

    public class BlackholeProjectile : YoyoProjectile, IBlackholeProjectile
    {
        public override string Texture { get => ModAssets.ProjectilesPath + "Blackhole"; }

        bool IBlackholeProjectile.IsActive => Projectile?.active ?? false;

        public BlackholeProjectile() : base(lifeTime: -1f, maxRange: 300f, topSpeed: 13f) { }

        public override void OnSpawn(IEntitySource source)
        {
            ModContent.GetInstance<BlackholeRenderTargetContent>().AddProjectile(this);
        }

        public override void YoyoOnHitNPC(Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsReturning) return;

            OnHitParticles();
        }

        public override void YoyoOnHitPlayer(Player owner, Player target, Player.HurtInfo info)
        {
            if (IsReturning) return;

            OnHitParticles();
        }

        private void OnHitParticles()
        {
            var particleSystem = ModContent.GetInstance<BlackholeParticleSystem>();

            for (int i = 0; i < 12; i++)
            {
                var position = Projectile.Center + Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Main.rand.NextFloat(20);
                var velocity = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Main.rand.NextFloat(1);
                var particle = new BlackholeParticle(position, velocity);

                particleSystem.AddParticle(particle);
            }
        }

        void IBlackholeProjectile.DrawSpaceMask()
        {
            //var scale = RadiusProgress * (YoyoGloveActivated ? 1.25f : 1f) * Projectile.scale;
            /*var scale = 1f * (YoyoGloveActivated ? 1.25f : 1f) * Projectile.scale;
            var drawPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModAssets.GetExtraTexture(30);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, 0f, texture.Size() * 0.5f, 0.45f * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, 0f, texture.Size() * 0.5f, 0.40f * scale, SpriteEffects.None, 0f);*/

            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Spiral", AssetRequestMode.ImmediateLoad);
            var scale = 1f * (YoyoGloveActivated ? 1.25f : 1f) * Projectile.scale;
            var drawPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, Main.GlobalTimeWrappedHourly * 2.5f, texture.Size() * 0.5f, 0.7f * (1 + MathF.Sin(Main.GlobalTimeWrappedHourly) * 0.1f) * scale, SpriteEffects.None, 0f);
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
        }

        public void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
        {
            var easeResult = EaseFunctions.InOutCirc(Progress);
            var color = Color.White * easeResult * (1f - easeResult) * 20f;
            spriteBatch.Draw(texture.Value, settings.AnchorPosition + position, null, color, rotation, origin, 0.5f, SpriteEffects.None, 0f);
        }
    }

    [Autoload(Side = ModSide.Client)]
    public class BlackholeParticleSystem : ModSystem
    {
        public bool AnyParticle { get => renderer.Particles.Count > 0; }

        private readonly ParticleRenderer renderer;

        public BlackholeParticleSystem()
        {
            renderer = new ParticleRenderer();
        }

        public override void PostUpdateEverything()
        {
            renderer.Update();
        }

        public override void OnWorldUnload()
        {
            renderer.Particles.Clear();
        }

        public void AddParticle(BlackholeParticle particle)
        {
            renderer.Add(particle);
        }

        public void DrawParticles()
        {
            renderer.Settings.AnchorPosition = -Main.screenPosition;
            renderer.Draw(Main.spriteBatch);
        }
    }

    public interface IBlackholeProjectile
    {
        bool IsActive { get; }
        void DrawSpaceMask();
    }

    public class BlackholeRenderTargetContent : RenderTargetContent
    {
        public override Point Size { get => new(Main.screenWidth / 2, Main.screenHeight / 2); }
        public BlackholeParticleSystem ParticleSystem { get => ModContent.GetInstance<BlackholeParticleSystem>(); }

        private Asset<Effect> effect;
        private List<IBlackholeProjectile> projectiles;

        public void AddProjectile(IBlackholeProjectile proj)
        {
            projectiles.Add(proj);
        }

        public override void Load()
        {
            projectiles = new();

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
            => ParticleSystem.AnyParticle || projectiles.Count > 0;

        public override void DrawToTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix);

            ParticleSystem.DrawParticles();

            for (int i = 0; i < projectiles.Count; i++)
            {
                if (!projectiles[i]?.IsActive ?? true)
                {
                    projectiles.RemoveAt(i);
                    i--;
                    break;
                }

                projectiles[i].DrawSpaceMask();
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