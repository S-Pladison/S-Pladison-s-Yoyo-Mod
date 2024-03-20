using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Graphics.PixelatedLayers;
using SPYoyoMod.Common.Graphics.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Entities;
using SPYoyoMod.Utils.Rendering;
using System;
using Terraria;
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

            Item.rare = ItemRarityID.Yellow;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    public class BlackholeProjectile : YoyoProjectile
    {
        public const float GravityRadius = 16 * 10;

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

        public void OnInitialize()
        {
            if (Main.dedServ) return;

            ModContent.GetInstance<BlackholeRenderTargetContent>().AddProjectile(Projectile);
        }

        public void SuckNearbyEnemies()
        {
            var currentRadius = GravityRadius * EasingFunctions.InOutSine(RadiusProgress);
            var targets = NPCUtils.NearestNPCs(
                center: Projectile.Center,
                radius: currentRadius,
                predicate: npc =>
                    npc.CanBeChasedBy(Projectile, false) &&
                    !npc.IsBossOrRelated() &&
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

        public override void AI()
        {
            if (!initialized)
            {
                OnInitialize();

                initialized = true;
            }

            if (IsReturning) RadiusProgress = 1f - ReturnToPlayerProgress;
            else RadiusProgress = MathHelper.Min(RadiusProgress + 0.05f, 1f);

            if (!Main.dedServ)
            {
                var position = Projectile.Center + Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Main.rand.NextFloat(20);
                var velocity = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Main.rand.NextFloat(0.75f);
                var particle = new BlackholeParticle(position, velocity, 1f - ReturnToPlayerProgress);

                ModContent.GetInstance<BlackholeRenderTargetContent>().AddParticle(particle);
            }

            if (!IsReturning && Main.myPlayer == Projectile.owner)
            {
                SuckNearbyEnemies();
            }
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
            DrawUtils.DrawGradientYoyoStringWithShadow(Projectile, mountedCenter, (Color.Transparent, true), (new Color(230, 135, 243), true));
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
    }

    public class BlackholeParticle : IParticle
    {
        public static readonly EasingBuilder ProgressEasing = new(
            (EasingFunctions.InOutCirc, 0.05f, 0f, 1f),
            (EasingFunctions.Linear, 0.95f, 1f, 0f)
        );

        public bool ShouldBeRemovedFromRenderer => timeLeft <= 0;
        public float Progress => MathHelper.Lerp(1f, 0f, timeLeft / (float)initTimeLeft);
        public Vector2 Position => position;

        private readonly Asset<Texture2D> texture;
        private readonly Vector2 origin;
        private readonly Vector2 initVelocity;
        private readonly int initTimeLeft;
        private readonly float rotation;
        private readonly float scale;

        private Vector2 position;
        private Vector2 velocity;
        private int timeLeft;

        public BlackholeParticle(Vector2 position, Vector2 velocity, float scale)
        {
            texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Smoke", AssetRequestMode.ImmediateLoad);
            origin = texture.Size() * 0.5f;
            timeLeft = initTimeLeft = Main.rand.Next(20, 40);
            rotation = Main.rand.NextFloat(MathHelper.TwoPi);

            this.position = position;
            this.velocity = initVelocity = velocity;
            this.scale = scale;
        }

        public void Update(ref ParticleRendererSettings settings)
        {
            velocity = Vector2.Lerp(initVelocity, Vector2.Zero, Progress);
            position += velocity;
            timeLeft--;
        }

        public void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
        {
            var color = Color.White * ProgressEasing.Evaluate(Progress);

            spriteBatch.Draw(texture.Value, settings.AnchorPosition + position, null, color, rotation, origin, 0.35f * scale, SpriteEffects.None, 0f);
        }
    }

    public class BlackholeRenderTargetContent : RenderTargetContent
    {
        public override Point Size => new(Main.screenWidth / 2, Main.screenHeight / 2);

        private ProjectileObserver projectileObserver;
        private ParticleRenderer particleRenderer;
        private Asset<Effect> effect;

        public override void Load()
        {
            projectileObserver = new(p => p.ModProjectile is not BlackholeProjectile);
            particleRenderer = new();

            ModEvents.OnPostUpdateEverything += projectileObserver.Update;
            ModEvents.OnWorldUnload += projectileObserver.Clear;

            ModEvents.OnPostUpdateEverything += particleRenderer.Update;
            ModEvents.OnWorldUnload += particleRenderer.Clear;

            ModEvents.OnPreDraw += EmitLight;

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

        public void EmitLight()
        {
            if (!projectileObserver.AnyEntity && particleRenderer.Particles.Count == 0) return;

            // Set Main.gamePaused to false to correctly emit light like torch and etc.
            // Not sure if it's safe, but I didn't find any errors

            // Lighting.AddLight(...)
            // {
            //     if (!Main.gamePaused && Main.netMode != 2)
            //     {
            //         _activeEngine.AddLight(...);
            //     }
            // }

            var origGamePaused = Main.gamePaused;
            Main.gamePaused = false;

            foreach (var yoyoProj in projectileObserver.GetEntityInstances())
            {
                Lighting.AddLight(yoyoProj.Center, new Color(171, 97, 255).ToVector3() * 0.6f);
            }

            for (int i = 0; i < particleRenderer.Particles.Count; i++)
            {
                var particle = particleRenderer.Particles[i] as BlackholeParticle;

                var lightColorMult = 0.4f * BlackholeParticle.ProgressEasing.Evaluate(particle.Progress);

                Lighting.AddLight(particle.Position, new Color(171, 97, 255).ToVector3() * lightColorMult);
            }

            Main.gamePaused = origGamePaused;
        }

        public override bool PreRender()
        {
            return projectileObserver.AnyEntity || particleRenderer.Particles.Count > 0;
        }

        public override void DrawToTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix);

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
            if (!WasRenderedInThisFrame || !TryGetRenderTarget(out var target)) return;

            effect ??= ModAssets.RequestEffect("BlackholeBackground").Prepare(parameters =>
            {
                var spaceTexture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Space", AssetRequestMode.ImmediateLoad);
                var cloudsTexture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Clouds", AssetRequestMode.ImmediateLoad);

                parameters["Texture1"].SetValue(spaceTexture.Value);
                parameters["Texture1Size"].SetValue(spaceTexture.Size());
                parameters["Texture2"].SetValue(cloudsTexture.Value);
                parameters["Texture2Size"].SetValue(cloudsTexture.Size() * 4);
                parameters["Cloud1Color"].SetValue(new Color(198, 50, 189).ToVector4());
                parameters["Cloud2Color"].SetValue(new Color(25, 25, 76).ToVector4());
            });

            effect.Prepare(parameters =>
            {
                parameters["Texture0Size"].SetValue(renderTarget.Size());
                parameters["EffectMatrix"].SetValue(Main.GameViewMatrix.EffectMatrix);
                parameters["ScreenPosition"].SetValue(Main.screenPosition + Main.ScreenSize.ToVector2() * 0.5f);
                parameters["Time"].SetValue((float)Main.timeForVisualEffects * 0.1f);
            });

            Main.spriteBatch.End(out var spriteBatchSnapshot);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, effect.Value, Main.GameViewMatrix.ZoomMatrix);
            Main.spriteBatch.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(spriteBatchSnapshot);
        }
    }
}