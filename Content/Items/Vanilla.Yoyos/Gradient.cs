using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Content.Items.Mod.Yoyos;
using SPYoyoMod.Content.Particles;
using SPYoyoMod.Core.Graphics;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class GradientAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Gradient/Gradient";

        public const string DaggerPath = $"{YoyoPath}_Dagger";
        public const string InvisiblePath = $"{AssetPath}/Invisible";

        public static readonly LazyAsset<Texture2D> DaggerGlowTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_DaggerGlow");
        public static readonly LazyAsset<Texture2D> FlameTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Flame");
        public static readonly LazyAsset<Texture2D> StarTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Star");
        public static readonly LazyAsset<Effect> GodraysEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Godrays", AssetRequestMode.ImmediateLoad);
        public static readonly LazyAsset<Effect> TrailEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Trail");
    }

    public sealed class GradientItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Gradient;
    }

    public sealed class GradientProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Gradient;

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.rand.NextBool(5))
                return;

            if (target.life <= 0)
                return;

            var godraysType = ModContent.ProjectileType<GradientGodraysProjectile>();

            if (proj.GetOwner().ownedProjectileCounts[godraysType] > 0)
                return;

            foreach (var otherProj in Main.ActiveProjectiles)
            {
                if (otherProj.type != godraysType)
                    continue;

                if ((otherProj.As<GradientGodraysProjectile>().Target?.whoAmI ?? -1) == target.whoAmI)
                    return;
            }

            Projectile.NewProjectile(proj.GetSource_OnHit(proj), target.Center, Vector2.Zero, godraysType, proj.damage, proj.knockBack, proj.owner, target.whoAmI);

            proj.GetOwner().ownedProjectileCounts[godraysType]++;
        }
    }

    public sealed class GradientGodraysProjectile : ModProjectile, IInitializableProjectile, IEmitLightProjectile
    {
        public static readonly int InitTimeLeft = ModUtils.SecondsToTicks(3f);
        public static readonly EasingBuilder OpacityEasing = new(
            (EasingFunctions.InOutQuad, 0.07f, 0f, 1f),
            (EasingFunctions.Linear, 0.78f, 1f, 1f),
            (EasingFunctions.InOutQuad, 0.15f, 1f, 0f)
        );

        private StripRenderer _stripRenderer;

        public override string Texture { get => GradientAssets.InvisiblePath; }
        public NPC Target { get => (int)Projectile.ai[0] >= 0 ? Main.npc[(int)Projectile.ai[0]] : null; }
        public float LifeTimeRatio { get => 1f - Projectile.timeLeft / (float)InitTimeLeft; }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = TileUtils.TileSizeInPixels * 105;
        }

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;

            Projectile.timeLeft = InitTimeLeft;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;

            Projectile.hide = true;
        }

        void IInitializableProjectile.Initialize(Projectile proj)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _stripRenderer = new StripRenderer(Main.graphics.GraphicsDevice, 2);

            SoundEngine.PlaySound(SoundID.DD2_BetsyWindAttack with { Pitch = 1f, }, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _stripRenderer?.Dispose();
        }

        public override void AI()
        {
            if (Target is null || !Target.active)
            {
                var deathTimeLeft = (int)(InitTimeLeft * 0.15f); //< 1.00 - (0.07 + 0.78) из OpacityEasing

                if (Projectile.timeLeft > deathTimeLeft)
                    Projectile.timeLeft = deathTimeLeft;

                return;
            }

            Projectile.Center = Target.Center;

            if (LifeTimeRatio > 0.75f)
                return;

            if (Projectile.IsLocalPlayerAsOwner() && Projectile.timeLeft % 5 == 0)
            {
                var daggerVelocity = Vector2.UnitY * 12;

                var daggerAimOffset = (Target.position - Target.oldPosition);
                daggerAimOffset.X += MathF.Sign(daggerAimOffset.X) * daggerVelocity.Y * 3; //< 3 из-за proj.extraUpdates
                daggerAimOffset.X = MathHelper.Clamp(daggerAimOffset.X, -TileUtils.TileSizeInPixels * 3, TileUtils.TileSizeInPixels * 3);

                var daggerPosition = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-TileUtils.TileSizeInPixels * 3.5f, TileUtils.TileSizeInPixels * 3.5f), -TileUtils.TileSizeInPixels * 45) + daggerAimOffset;

                Projectile.NewProjectile(Projectile.GetSource_FromAI(), daggerPosition, daggerVelocity, ModContent.ProjectileType<GradientDaggerProjectile>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.Top.Y);
            }

            if (Projectile.timeLeft % 4 == 0)
            {
                var particle = WorldParticleManager.SpawnParticle<LightPointParticle>();

                particle.LifeTime = ModUtils.SecondsToTicks(1.5f);
                particle.Position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-TileUtils.TileSizeInPixels * 4, TileUtils.TileSizeInPixels * 4), Main.rand.NextFloat(-TileUtils.TileSizeInPixels * 70, 6));
                particle.Velocity = Main.rand.NextVector2Circular(0.2f, 0.5f);
                particle.StartColor = new Color(255, 250, 185);
                particle.EndColor = new Color(255, 190, 0);
                particle.Scale = Main.rand.NextFloat(0.2f, 0.3f);
            }
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        void IEmitLightProjectile.EmitLight(Projectile _)
        {
            var opacity = OpacityEasing.Evaluate(LifeTimeRatio);

            if (opacity <= 0f)
                return;

            Lighting.AddLight(Projectile.Center, new Color(255, 190, 0).ToVector3() * opacity * 0.5f);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override void PostDraw(Color lightColor)
        {
            if (_stripRenderer is null)
                return;

            var opacity = OpacityEasing.Evaluate(LifeTimeRatio);

            if (opacity <= 0f)
                return;

            var position = Projectile.Bottom + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var lightningStartPosition = position - Vector2.UnitY * TileUtils.TileSizeInPixels * 80;
            var lightningEndPosition = position;

            GradientAssets.GodraysEffect
                .Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(TextureAssets.MagicPixel.Value);
                    parameters["TransformMatrix"].SetValue(GameMatrices.Transform * GameMatrices.Projection);
                    parameters["Position"].SetValue(Projectile.Bottom);
                    parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly + Projectile.whoAmI * 12.345f);
                    parameters["Opacity"].SetValue(opacity);
                })
                .Apply();

            _stripRenderer
                .SetWidth(TileUtils.TileSizeInPixels * 16)
                .SetPoints([lightningStartPosition, lightningEndPosition])
                .Render();
        }
    }

    public sealed class GradientDaggerProjectile : ModProjectile, IInitializableProjectile
    {
        public static readonly int InitTimeLeft = ModUtils.SecondsToTicks(2f);
        public static readonly int TrailPointCount = 4;
        public static readonly EasingBuilder OpacityEasing = new(
            (EasingFunctions.InOutQuad, 0.25f, 0f, 1f),
            (EasingFunctions.Linear, 0.65f, 1f, 1f),
            (EasingFunctions.InOutQuad, 0.1f, 1f, 0f)
        );

        private StripRenderer _trailRenderer;
        private LinkedList<Vector2> _oldPositions;

        public override string Texture { get => GradientAssets.DaggerPath; }
        public ref float HeightToBecomeCollidable { get => ref Projectile.ai[0]; }
        public bool WasCollided { get => Projectile.ai[0] <= 1; }
        public float LifeTimeRatio { get => 1f - Projectile.timeLeft / (float)InitTimeLeft; }

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = 6;
            Projectile.height = 6;

            Projectile.timeLeft = InitTimeLeft;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 2;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        void IInitializableProjectile.Initialize(Projectile _)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _trailRenderer = new StripRenderer(Main.graphics.GraphicsDevice, capacity: TrailPointCount)
            {
                StartWidth = 20,
                EndWidth = 10
            };

            _oldPositions = [];
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _trailRenderer?.Dispose();

            SoundEngine.PlaySound(SoundID.DD2_SkyDragonsFuryShot with
            {
                Pitch = 1f,
                PitchVariance = 0.2f
            }, Projectile.Center);
        }

        public override void AI()
        {
            if (_trailRenderer is not null && Projectile.numUpdates == -1)
            {
                _oldPositions.AddFirst(Projectile.Center);

                while (_oldPositions.Count > TrailPointCount)
                    _oldPositions.RemoveLast();

                _trailRenderer.SetPoints(_oldPositions);
            }

            if (!Projectile.tileCollide && Projectile.Bottom.Y >= HeightToBecomeCollidable)
            {
                Projectile.tileCollide = true;
            }

            if (WasCollided)
            {
                if (Projectile.timeLeft > 15)
                {
                    Projectile.localAI[0] = 1f;
                    Projectile.extraUpdates = 0;
                    Projectile.timeLeft = 15;
                }

                // Если столкновение было с NPC
                if (Projectile.ai[0] <= 0f)
                {
                    var target = Main.npc[-(int)Projectile.ai[0]];

                    if (target.active)
                        Projectile.position += target.position - target.oldPosition;
                    else
                        Projectile.Kill();
                }
            }

            if (Projectile.timeLeft == 7)
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Bottom, Vector2.Zero, ModContent.ProjectileType<GradientStarProjectile>(), 0, 0, Projectile.owner);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathF.Sin(Projectile.localAI[0] * MathHelper.TwoPi) * (Projectile.whoAmI % 2 == 0 ? 1 : -1) * 0.1f;
            Projectile.localAI[0] = MathHelper.Max(Projectile.localAI[0] - 0.15f, 0f);
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            hitbox.Inflate(3, 3);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.GetOwner().Counterweight(target.Center, Projectile.damage, Projectile.knockBack);

            if (WasCollided)
                return;

            var vector = Vector2.Normalize(Projectile.velocity);

            Projectile.velocity = vector * 0.0001f;
            Projectile.Center -= vector * 4f;
            Projectile.ai[0] = -target.whoAmI; //< WasCollided = true
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = oldVelocity;

            if (WasCollided)
                return false;

            var vector = Vector2.Normalize(oldVelocity);

            Projectile.velocity = vector * 0.001f;
            Projectile.Center += vector * Main.rand.NextFloat(5f, 12f);
            Projectile.ai[0] = 1; //< WasCollided = true
            Projectile.hide = true;

            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color _)
        {
            var opacity = OpacityEasing.Evaluate(LifeTimeRatio);

            if (_trailRenderer is not null)
            {
                GradientAssets.TrailEffect
                    .Prepare(parameters =>
                    {
                        parameters["Texture0"].SetValue(GradientAssets.FlameTexture.Value);
                        parameters["TransformMatrix"].SetValue(GameMatrices.World * GameMatrices.Transform * GameMatrices.Projection);
                        parameters["Color0"].SetValue(Color.White.ToVector4());
                        parameters["Color1"].SetValue(new Color(195, 165, 10).ToVector4());
                        parameters["Repeats"].SetValue(_trailRenderer.Points.Distance() / GradientAssets.FlameTexture.Value.Width / 128.0f / 4.0f);
                        parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly + Projectile.whoAmI * 15.08f);
                        parameters["Opacity"].SetValue(opacity);
                    })
                    .Apply();

                _trailRenderer.Render();
            }

            var position = Projectile.Center - Main.screenPosition;

            var glowTexture = GradientAssets.DaggerGlowTexture;
            var glowOrigin = glowTexture.Value.Size() * 0.5f;
            var glowColor = new Color(120, 110, 60, 0) * opacity * 0.5f;

            Main.spriteBatch.Draw(glowTexture.Value, position, null, glowColor, Projectile.rotation, glowOrigin, Projectile.scale, SpriteEffects.None, 0);

            var daggerTexture = TextureAssets.Projectile[Type];
            var daggerOrigin = daggerTexture.Size() * 0.5f;

            // Хотя функция и передает параметр цвета, его значение не является истинным, поэтому определяем сами
            var daggerColor = Color.Lerp(Lighting.GetColor(Projectile.Center.ToTileCoordinates(), Color.White), Color.White, 0.4f) * opacity;

            Main.spriteBatch.Draw(daggerTexture.Value, position, null, daggerColor, Projectile.rotation, daggerOrigin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }

    public sealed class GradientStarProjectile : ModProjectile, IPostDrawPixelatedProjectile
    {
        public static readonly int InitTimeLeft = ModUtils.SecondsToTicks(0.2f);
        public static readonly EasingBuilder ScaleEasing = new(
            (EasingFunctions.InOutExpo, 0.4f, 0f, 1f),
            (EasingFunctions.InOutQuad, 0.6f, 1f, 0f)
        );

        public override string Texture { get => TheStellarThrowAssets.InvisiblePath; }
        public float LifeTimeRatio { get => 1f - Projectile.timeLeft / (float)InitTimeLeft; }

        public override void SetDefaults()
        {
            Projectile.DefaultToVisualEffect();

            Projectile.timeLeft = InitTimeLeft;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        void IPostDrawPixelatedProjectile.PostDrawPixelated(Projectile _)
        {
            var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;

            var starTexture = GradientAssets.StarTexture.Value;
            var starOrigin = starTexture.Size() * 0.5f;
            var starScale = ScaleEasing.Evaluate(LifeTimeRatio);

            Main.spriteBatch.Draw(starTexture, position, null, Color.Black * 0.5f, Projectile.rotation * 0.05f, starOrigin, starScale * 0.55f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(starTexture, position, null, new Color(195, 165, 55) with { A = 0 }, Projectile.rotation * 0.1f, starOrigin, starScale * 0.4f, SpriteEffects.None, 0f);
        }
    }
}