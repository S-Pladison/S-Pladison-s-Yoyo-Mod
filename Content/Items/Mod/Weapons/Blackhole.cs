using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.Networking;
using SPYoyoMod.Common.PixelatedLayers;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Entities;
using SPYoyoMod.Utils.Rendering;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class BlackholeItem : YoyoItem
    {
        public override string Texture => ModAssets.ItemsPath + "Blackhole";
        public override int GamepadExtraRange => 15;

        public override void YoyoSetStaticDefaults()
        {
            ModSets.Items.InventoryDrawScaleMultiplier[Type] = 1.3f;
        }

        public override void YoyoSetDefaults()
        {
            Item.width = 42;
            Item.height = 26;

            Item.damage = 43;
            Item.knockBack = 2.5f;

            Item.shoot = ModContent.ProjectileType<BlackholeProjectile>();

            Item.rare = ItemRarityID.Lime;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    public class BlackholeProjectile : YoyoProjectile, IDrawDistortionProjectile
    {
        public static readonly float GravityRadius = GravityRadius = 16 * 10;

        public override string Texture => ModAssets.ProjectilesPath + "Blackhole";
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;
        public float TimeForVisualEffects => (Projectile.whoAmI * 200f + (float)Main.timeForVisualEffects) % 216000f;

        public float RadiusProgress
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        private bool initialized;

        public override void AI()
        {
            if (!initialized)
            {
                ModContent.GetInstance<BlackholeRenderTargetContent>()?.AddProjectile(Projectile);
                initialized = true;
            }

            Lighting.AddLight(Projectile.Center, new Color(171, 97, 255).ToVector3() * 0.6f);

            if (IsReturning) return;

            RadiusProgress += !IsReturning ? 0.05f : -0.1f;
            RadiusProgress = Math.Clamp(RadiusProgress, 0, 1);

            var currentRadius = GravityRadius * EasingFunctions.InOutSine(RadiusProgress);
            var targets = NPCUtils.NearestNPCs(
                center: Projectile.Center,
                radius: currentRadius,
                predicate: (npc) =>
                    npc.CanBeChasedBy(Projectile, false) &&
                    !npc.boss &&
                    !NPCID.Sets.ShouldBeCountedAsBoss[npc.type] &&
                    Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height)
            );

            foreach ((var npc, var distance) in targets)
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
            if (IsReturning) return;

            if (!Main.dedServ)
            {
                var blackholeRTContent = ModContent.GetInstance<BlackholeRenderTargetContent>();

                for (var i = 0; i < 7; i++)
                {
                    var position = Projectile.Center + Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Main.rand.NextFloat(20);
                    var velocity = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Main.rand.NextFloat(0.75f);
                    var particle = new BlackholeParticle(position, velocity);

                    blackholeRTContent.AddParticle(particle);
                }
            }

            if (Projectile.owner == Main.myPlayer)
                new ModProjectileOnHitNPCPacket(Projectile.identity, Projectile.type, target.whoAmI, target.type).Send();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.OverProjectiles, () =>
            {
                var drawPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Blackhole_LensFlare", AssetRequestMode.ImmediateLoad);
                var rotation = MathF.Sin(TimeForVisualEffects * 0.01f);
                var scale = Projectile.scale;

                Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, rotation, texture.Size() * 0.5f, 0.85f * scale, SpriteEffects.None, 0f);
            });

            return true;
        }

        public override void PostDrawYoyoString(Vector2 mountedCenter)
        {
            DrawUtils.DrawYoyoString(Projectile, mountedCenter, (segmentCount, segmentIndex, position, rotation, height, color) =>
            {
                var pos = position - Main.screenPosition;
                var rect = new Rectangle(0, 0, TextureAssets.FishingLine.Width(), (int)height);
                var origin = new Vector2(TextureAssets.FishingLine.Width() * 0.5f, 0f);
                var colour = Color.Lerp(Color.Transparent, new Color(230, 135, 243), EasingFunctions.InQuart(segmentIndex / (float)segmentCount) * 2f);

                Main.spriteBatch.Draw(TextureAssets.FishingLine.Value, pos, rect, colour, rotation, origin, 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            });
        }

        public void DrawSpaceMask()
        {
            var scale = Projectile.scale * EasingFunctions.InOutSine(RadiusProgress);
            var drawPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Circle", AssetRequestMode.ImmediateLoad);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, 0f, texture.Size() * 0.5f, 0.64f * scale, SpriteEffects.None, 0f);

            texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Smoke", AssetRequestMode.ImmediateLoad);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, TimeForVisualEffects * 0.02f, texture.Size() * 0.5f, 0.52f * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, TimeForVisualEffects * 0.01f, texture.Size() * 0.5f, 0.47f * scale, SpriteEffects.FlipHorizontally, 0f);

            texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Spiral", AssetRequestMode.ImmediateLoad);
            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.White, TimeForVisualEffects * 0.02f, texture.Size() * 0.5f, 0.32f * scale, SpriteEffects.None, 0f);
        }

        void IDrawDistortionProjectile.DrawDistortion(Projectile proj)
        {
            var drawPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "DistortedRing", AssetRequestMode.ImmediateLoad);
            var rotation = TimeForVisualEffects * 0.02f;
            var scale = Projectile.scale * EasingFunctions.InOutSine(RadiusProgress);

            Main.spriteBatch.Draw(texture.Value, drawPosition, null, Color.Gray, rotation, texture.Size() * 0.5f, 0.6f * scale, SpriteEffects.None, 0f);
        }
    }

    public class BlackholeParticle : IParticle
    {
        private static readonly EasingBuilder progressEasing = new(
            (EasingFunctions.InOutCirc, 0.2f, 0f, 1f),
            (EasingFunctions.Linear, 0.6f, 1f, 1f),
            (EasingFunctions.InOutCirc, 0.2f, 1f, 0f)
        );

        public bool ShouldBeRemovedFromRenderer => timeLeft <= 0;
        public float Progress => MathHelper.Lerp(1f, 0f, timeLeft / (float)initTimeLeft);

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
            texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Smoke", AssetRequestMode.ImmediateLoad);
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

            var lightColorMult = 0.4f * progressEasing.Evaluate(Progress);
            Lighting.AddLight(position, new Color(171, 97, 255).ToVector3() * lightColorMult);
        }

        public void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
        {
            var color = Color.White * progressEasing.Evaluate(Progress);
            spriteBatch.Draw(texture.Value, settings.AnchorPosition + position, null, color, rotation, origin, 0.35f, SpriteEffects.None, 0f);
        }
    }

    public class BlackholeRenderTargetContent : RenderTargetContent
    {
        public override Point Size => new(Main.screenWidth / 2, Main.screenHeight / 2);

        private Asset<Effect> effect;
        private ProjectileObserver projectileObserver;
        private ParticleRenderer particleRenderer;

        public override void Load()
        {
            projectileObserver = new(p => p.ModProjectile is not BlackholeProjectile);
            particleRenderer = new();

            ModEvents.OnPostUpdateEverything += projectileObserver.Update;
            ModEvents.OnWorldUnload += projectileObserver.Clear;

            ModEvents.OnPostUpdateEverything += particleRenderer.Update;
            ModEvents.OnWorldUnload += particleRenderer.Clear;

            On_Main.DoDraw_WallsAndBlacks += (orig, main) =>
            {
                orig(main);
                DrawToScreen();
            };
        }

        public void AddProjectile(Projectile proj)
        {
            projectileObserver.Add(proj);
        }

        public void AddParticle(BlackholeParticle particle)
        {
            particleRenderer.Add(particle);
        }

        public override bool PreRender()
        {
            return projectileObserver.AnyEntity || particleRenderer.Particles.Count > 0;
        }

        public override void DrawToTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix);

            particleRenderer.Settings.AnchorPosition = -Main.screenPosition;
            particleRenderer.Draw(Main.spriteBatch);

            foreach (var proj in projectileObserver.GetEntityInstances())
            {
                (proj.ModProjectile as BlackholeProjectile).DrawSpaceMask();
            }

            Main.spriteBatch.End();
        }

        public void DrawToScreen()
        {
            if (!IsRenderedInThisFrame || !TryGetRenderTarget(out var target)) return;

            effect ??= LoadEffect();

            var parameters = effect.Value.Parameters;
            parameters["Texture0Size"].SetValue(renderTarget.Size());
            parameters["EffectMatrix"].SetValue(Main.GameViewMatrix.EffectMatrix);
            parameters["ScreenPosition"].SetValue(Main.screenPosition + Main.ScreenSize.ToVector2() * 0.5f);
            parameters["Time"].SetValue((float)Main.timeForVisualEffects * 0.1f);

            Main.spriteBatch.End(out var spriteBatchSnapshot);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, effect.Value, Main.GameViewMatrix.ZoomMatrix);
            Main.spriteBatch.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(spriteBatchSnapshot);
        }

        public static Asset<Effect> LoadEffect()
        {
            var spaceTexture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Space", AssetRequestMode.ImmediateLoad);
            var cloudsTexture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Clouds", AssetRequestMode.ImmediateLoad);
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