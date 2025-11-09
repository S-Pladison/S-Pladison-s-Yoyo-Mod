using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Content.Particles;
using SPYoyoMod.Core.Graphics;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    public sealed class TheStellarThrowAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Mod.Yoyos/TheStellarThrow/TheStellarThrow";

        public const string ItemPath = $"{YoyoPath}_Item";
        public const string ProjPath = $"{YoyoPath}_Proj";
        public const string InvisiblePath = $"{AssetPath}/Invisible";
        public const string StringPath = $"{AssetPath}/FishingLine_WithShadow";

        public static readonly LazyAsset<Texture2D> GlowTexture = LazyAsset<Texture2D>.From($"{AssetPath}/YoyoGlow_WithShadow");
        public static readonly LazyAsset<Texture2D> CircleTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Circle");
        public static readonly LazyAsset<Texture2D> StarTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Star");
        public static readonly LazyAsset<Texture2D> FlameTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Flame");
        public static readonly LazyAsset<Effect> TrailEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Trail");
    }

    public sealed class TheStellarThrowItem : YoyoBaseItem
    {
        public override string Texture => TheStellarThrowAssets.ItemPath;
        public override int GamepadExtraRange => 10;

        public override void SetDefaults()
        {
            base.SetDefaults();

            Item.damage = 18;
            Item.knockBack = 3f;

            Item.shoot = ModContent.ProjectileType<TheStellarThrowProjectile>();

            Item.rare = ItemRarityID.Green;
            Item.value = ItemUtils.SellPrice(platinum: 0, gold: 1, silver: 0, copper: 0);
        }
    }

    public sealed class TheStellarThrowProjectile : YoyoBaseProjectile, IInitializableProjectile, IPreDrawPixelatedProjectile, IEmitLightEntity
    {
        public static readonly float SpawnStarRadius = TileUtils.TileSizeInPixels * 15f;
        public static readonly int SpawnStarCooldownMin = GeneralUtils.SecondsToTicks(1.5f);
        public static readonly int SpawnStarCooldownMax = GeneralUtils.SecondsToTicks(2f);
        public static readonly Color GlowColor = new(252, 194, 116);
        public static readonly Color StarColor = new(255, 0, 80);
        public static readonly int TrailPointCount = 15;

        private int _cooldownTimer;
        private YoyoStringRenderer _stringRenderer;
        private StripRenderer _trailRenderer;
        private LinkedList<Vector2> _oldPositions;

        public override string Texture => TheStellarThrowAssets.ProjPath;
        public override float LifeTime => -1f;
        public override float MaxRange => 235f;
        public override float TopSpeed => 14f;

        public override void OnSpawn(IEntitySource source)
        {
            SetCooldownForStarSpawn();
        }

        void IInitializableProjectile.Initialize(Projectile proj)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _stringRenderer = new YoyoStringRenderer(new IDrawYoyoStringSegments.Gradient(
               ModContent.Request<Texture2D>(TheStellarThrowAssets.StringPath, AssetRequestMode.ImmediateLoad).Value,
               (Color.Transparent, true), (Color.Transparent, true), (GlowColor, true)
            ));

            _trailRenderer = new StripRenderer(Main.graphics.GraphicsDevice, capacity: TrailPointCount)
            {
                StartWidth = 45,
                EndWidth = 25
            };

            _oldPositions = [];
        }

        public override void OnKill(int timeLeft)
        {
            _trailRenderer?.Dispose();
        }

        public override void AI()
        {
            UpdateVisual();

            // Если снаряд не наш, то смысла обрабатывать его логику спавна звезд просто нет
            if (!Projectile.IsLocalPlayerAsOwner())
                return;

            if (--_cooldownTimer > 0)
                return;

            var nearbyNPCs = new List<NPC>();

            foreach (var npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                if (Vector2.Distance(npc.Center, Projectile.Center) > SpawnStarRadius)
                    continue;

                nearbyNPCs.Add(npc);
            }

            if (nearbyNPCs.Count == 0)
            {
                // Устанавливаем кулдаун на 5 тиков для того, чтобы не спамить каждый тик проверками на ближайших NPC
                SetCooldownForStarSpawn(5);
                return;
            }

            var target = nearbyNPCs[Main.rand.Next(nearbyNPCs.Count)];
            var starPosition = target.Center - new Vector2((Main.rand.NextBool() ? 1 : -1) * Main.rand.NextFloat(20f, 60f), 50f) * TileUtils.TileSizeInPixels;
            var starSpeed = 32f;
            var starDirection = ProjectileUtils.PredictiveAimToTarget(starPosition, target.Center, target.velocity, starSpeed);

            Projectile.NewProjectile(Projectile.GetSource_FromAI(), starPosition, starDirection * starSpeed, ModContent.ProjectileType<TheStellarThrowStarProjectile>(), Projectile.damage, Projectile.knockBack, Projectile.owner, target.whoAmI);

            SetCooldownForStarSpawn();
        }

        private void UpdateVisual()
        {
            if (_trailRenderer is not null)
            {
                _oldPositions.AddFirst(Projectile.Center + Projectile.velocity);

                while (_oldPositions.Count > TrailPointCount)
                    _oldPositions.RemoveLast();

                _trailRenderer.SetPoints(_oldPositions);
            }

            if (Projectile.velocity.Length() >= 3f && Main.rand.NextBool(4))
            {
                if (Main.rand.NextBool(3))
                {
                    var particle = WorldParticleManager.SpawnParticle<StarParticle>();

                    particle.LifeTime = GeneralUtils.SecondsToTicks(0.5f);
                    particle.Position = Projectile.Center + Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Projectile.width * Main.rand.NextFloat() * 0.5f;
                    particle.StartColor = new Color(255, 175, 65);
                    particle.EndColor = new Color(255, 85, 225);
                    particle.Scale = Main.rand.NextFloat(0.8f, 1.0f);
                }
                else
                {
                    var particle = WorldParticleManager.SpawnParticle<LightPointParticle>();

                    particle.LifeTime = GeneralUtils.SecondsToTicks(0.5f);
                    particle.Position = Projectile.Center + Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Projectile.width * Main.rand.NextFloat() * 0.5f;
                    particle.StartColor = new Color(255, 50, 160);
                    particle.EndColor = new Color(50, 50, 255);
                    particle.Scale = Main.rand.NextFloat(0.3f, 0.4f);
                }
            }

            Projectile.rotation -= 0.15f;
        }

        private void SetCooldownForStarSpawn(int? cooldown = null)
        {
            _cooldownTimer = cooldown ?? Main.rand.Next(SpawnStarCooldownMin, SpawnStarCooldownMax);
        }

        void IEmitLightEntity.EmitLight(Entity _)
        {
            Lighting.AddLight(Projectile.Center, StarColor.ToVector3() * 0.2f);
        }

        public override Color? GetAlpha(Color lightColor)
            => Color.White;

        void IPreDrawPixelatedProjectile.PreDrawPixelated(Projectile proj)
        {
            if (_trailRenderer is not null)
            {
                TheStellarThrowAssets.TrailEffect
                    .Prepare(parameters =>
                    {
                        parameters["Texture0"].SetValue(TheStellarThrowAssets.FlameTexture.Value);
                        parameters["TransformMatrix"].SetValue(GameMatrices.World * GameMatrices.Effect * GameMatrices.Projection);
                        parameters["Color0"].SetValue(new Color(255, 50, 160).ToVector4());
                        parameters["Color1"].SetValue(new Color(170, 30, 90).ToVector4());
                        parameters["Color2"].SetValue(new Color(50, 50, 255).ToVector4());
                        parameters["Color3"].SetValue(new Color(60, 55, 90).ToVector4());
                        parameters["Repeats"].SetValue(_trailRenderer.Points.Distance() / TheStellarThrowAssets.FlameTexture.Value.Width / 128.0f / 4.0f);
                        parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                    })
                    .Apply();

                _trailRenderer.Render();
            }

            var starTexture = TheStellarThrowAssets.StarTexture.Value;
            var starPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var starOrigin = starTexture.Size() * 0.5f;
            var starColor = new Color(100, 25, 75) * 0.35f;

            Main.spriteBatch.Draw(starTexture, starPosition, null, starColor, Projectile.rotation * 0.05f, starOrigin, proj.scale * 0.6f, SpriteEffects.None, 0f);

            starColor = StarColor with { A = 0 };

            Main.spriteBatch.Draw(starTexture, starPosition, null, starColor, Projectile.rotation * 0.1f, starOrigin, proj.scale * 0.4f, SpriteEffects.None, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var glowPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var glowTexture = TheStellarThrowAssets.GlowTexture.Value;
            var glowOrigin = glowTexture.Size() * 0.5f;
            var glowScale = Projectile.scale * 1.2f;

            Main.spriteBatch.Draw(glowTexture, glowPosition, null, GlowColor, Projectile.rotation, glowOrigin, glowScale, SpriteEffects.None, 0f);

            return true;
        }

        public override void PostDrawYoyoString(Vector2 mountedCenter)
        {
            if (_stringRenderer is null)
                return;

            var settings = new YoyoStringRendererSettings(
                proj: Projectile,
                start: mountedCenter + Projectile.GetOwner()?.gfxOffY * Vector2.UnitY ?? Vector2.Zero,
                offset: -Main.screenPosition
            );

            _stringRenderer.Render(Main.spriteBatch, settings);
        }
    }

    public readonly struct TheStellarThrowPalette(Color starFirst, Color starSecond, Color starThird, Color trailStartOne, Color trailStartZero, Color trailEndOne, Color trailEndZero)
    {
        public readonly Color StarFirst = starFirst; //< Основной цвет звезды
        public readonly Color StarSecond = starSecond; //< Цвет *обводки* звезды
        public readonly Color StarThird = starThird; //< Цвет *тени*

        public readonly Color TrailStartOne = trailStartOne;
        public readonly Color TrailStartZero = trailStartZero;
        public readonly Color TrailEndOne = trailEndOne;
        public readonly Color TrailEndZero = trailEndZero;

        public static readonly TheStellarThrowPalette[] Palettes =
        [
            // Розовато-фиолетовый
            new(
                starFirst: new(255, 240, 185),
                starSecond: new(255, 0, 80),
                starThird: new(160, 30, 120),
                trailStartOne: new(255, 0, 80),
                trailStartZero: new(110, 10, 95),
                trailEndOne: new(110, 10, 95),
                trailEndZero: new(40, 15, 50)
            ),
            // Синий
            new(
                starFirst: new(185, 240, 255),
                starSecond: new(0, 135, 255),
                starThird: new(85, 30, 160),
                trailStartOne: new(185, 240, 255),
                trailStartZero: new(0, 135, 255),
                trailEndOne: new(0, 135, 255),
                trailEndZero: new(25, 40, 100)
            ),
            // Изумрудный
            new(
                starFirst: new(170, 255, 205),
                starSecond: new(25, 220, 125),
                starThird: new(30, 110, 160),
                trailStartOne: new(25, 220, 125),
                trailStartZero: new(30, 110, 160),
                trailEndOne: new(30, 110, 160),
                trailEndZero: new(15, 25, 100)
            ),
            // Золотой
            new(
                starFirst: new(250, 255, 185),
                starSecond: new(255, 135, 0),
                starThird: new(160, 30, 70),
                trailStartOne: new(250, 255, 185),
                trailStartZero: new(255, 135, 0),
                trailEndOne: new(255, 135, 0),
                trailEndZero: new(255, 0, 80)
            ),
        ];
    }

    public sealed class TheStellarThrowStarProjectile : ModProjectile, IInitializableProjectile, IPreDrawPixelatedProjectile, IEmitLightEntity
    {
        public static readonly int TrailPointCount = 20;
        public static readonly int NpcHitOutlineLifeTime = GeneralUtils.SecondsToTicks(0.25f);
        public static readonly EasingBuilder NpcHitOutlineThicknessEasing = new(
            (EasingFunctions.InOutExpo, 0.2f, 0f, 1f),
            (EasingFunctions.InOutQuad, 0.8f, 1f, 0f)
        );

        private float _yToBecomeCollidable;
        private StripRenderer _trailRenderer;
        private LinkedList<Vector2> _oldPositions;

        public override string Texture { get => TheStellarThrowAssets.InvisiblePath; }
        private int TargetIndex { get => (int)Projectile.ai[0]; }
        private int StyleIndex { get => (int)Projectile.ai[1]; set => Projectile.ai[1] = value; }
        private ref TheStellarThrowPalette Style { get => ref TheStellarThrowPalette.Palettes[StyleIndex]; }

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = 32;
            Projectile.height = 32;

            Projectile.timeLeft = 60 * 3;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            var heldItem = Projectile.GetOwner().HeldItem;

            if (heldItem is null || heldItem.type != ModContent.ItemType<TheStellarThrowItem>() || !heldItem.favorited)
            {
                StyleIndex = Main.rand.Next(0, 3);
                return;
            }

            StyleIndex = 3; //< Единственный, золотой цвет, если игрок отметил йо-йо как *избранный*
        }

        void IInitializableProjectile.Initialize(Projectile _)
        {
            Projectile.velocity /= 1 + Projectile.extraUpdates;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Projectile.scale = 0.01f;

            _yToBecomeCollidable = (TargetIndex >= 0 && Main.npc[TargetIndex] is NPC target && target.active) ? (target.Top.Y + 2) : 0f;

            if (Main.netMode == NetmodeID.Server)
                return;

            _trailRenderer = new StripRenderer(Main.graphics.GraphicsDevice, capacity: TrailPointCount)
            {
                StartWidth = 60,
                EndWidth = 35
            };

            _oldPositions = [];
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _trailRenderer?.Dispose();
        }

        public override void AI()
        {
            UpdateVisual();
            UpdateSound();

            if (!Projectile.tileCollide && Projectile.Center.Y >= _yToBecomeCollidable)
            {
                Projectile.tileCollide = true;
            }
        }

        private void UpdateVisual()
        {
            if (_trailRenderer is not null)
            {
                _oldPositions.AddFirst(Projectile.Center + Projectile.velocity);

                while (_oldPositions.Count > TrailPointCount)
                    _oldPositions.RemoveLast();

                _trailRenderer.SetPoints(_oldPositions);
            }

            if (Projectile.numUpdates == 0)
            {
                Projectile.rotation += 0.5f;
                Projectile.scale = MathHelper.Min(1f, Projectile.scale + 0.1f);
            }

            if (Projectile.velocity.Length() >= 3f && Main.rand.NextBool(4))
            {
                if (Main.rand.NextBool(3))
                {
                    var particle = WorldParticleManager.SpawnParticle<StarParticle>();

                    particle.LifeTime = GeneralUtils.SecondsToTicks(0.5f);
                    particle.Position = Projectile.Center + Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Projectile.width * Main.rand.NextFloat() * 0.5f;
                    particle.Velocity = Projectile.velocity * 0.05f;
                    particle.StartColor = Style.StarFirst;
                    particle.EndColor = Style.StarSecond;
                    particle.Scale = Main.rand.NextFloat(1.0f, 2.0f);
                }
                else
                {
                    var particle = WorldParticleManager.SpawnParticle<LightPointParticle>();

                    particle.LifeTime = GeneralUtils.SecondsToTicks(0.5f);
                    particle.Position = Projectile.Center + Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Projectile.width * Main.rand.NextFloat() * 0.5f;
                    particle.Velocity = Projectile.velocity * 0.05f;
                    particle.StartColor = Style.StarFirst;
                    particle.EndColor = Style.StarSecond;
                    particle.Scale = Main.rand.NextFloat(0.3f, 0.6f);
                }
            }
        }

        private void UpdateSound()
        {
            if (Projectile.numUpdates == 0 && Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = GeneralUtils.SecondsToTicks(Main.rand.NextFloat(1.0f, 2.0f));

                SoundEngine.PlaySound(in SoundID.Item9, Projectile.Center);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage += 1f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.tileCollide = true;
            Projectile.GetOwner().Counterweight(target.Center, Projectile.damage, Projectile.knockBack);

            if (target.life <= 0)
                return;

            NPCEffectManager.Outline(new NPCEffectManager.OutlineSettings()
            {
                NpcWhoAmI = target.whoAmI,
                LifeTime = NpcHitOutlineLifeTime,
                OutlineColor = static (lifeTimeRatio) => new Color(255, 0, 80) * NpcHitOutlineThicknessEasing.Evaluate(lifeTimeRatio)
            });
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = Vector2.Zero;

            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<TheStellarThrowHitProjectile>(), 0, 0, Projectile.owner, StyleIndex);

            return true;
        }

        void IEmitLightEntity.EmitLight(Entity _)
        {
            Lighting.AddLight(Projectile.Center, Style.StarSecond.ToVector3() * 0.3f);
        }

        void IPreDrawPixelatedProjectile.PreDrawPixelated(Projectile _)
        {
            if (_trailRenderer is not null)
            {
                TheStellarThrowAssets.TrailEffect
                    .Prepare(parameters =>
                    {
                        parameters["Texture0"].SetValue(TheStellarThrowAssets.FlameTexture.Value);
                        parameters["TransformMatrix"].SetValue(GameMatrices.World * GameMatrices.Effect * GameMatrices.Projection);
                        parameters["Color0"].SetValue(Style.TrailStartOne.ToVector4());
                        parameters["Color1"].SetValue(Style.TrailStartZero.ToVector4());
                        parameters["Color2"].SetValue(Style.TrailEndOne.ToVector4());
                        parameters["Color3"].SetValue(Style.TrailEndZero.ToVector4());
                        parameters["Repeats"].SetValue(_trailRenderer.Points.Distance() / TheStellarThrowAssets.FlameTexture.Value.Width / 128.0f / 4.0f);
                        parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                    })
                    .Apply();

                _trailRenderer.Render();
            }

            var starPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var starTexture = TheStellarThrowAssets.StarTexture.Value;
            var starOrigin = starTexture.Size() * 0.5f;

            Main.spriteBatch.Draw(starTexture, starPosition, null, Style.StarThird * 0.25f, Projectile.rotation * 0.05f, starOrigin, Projectile.scale * 0.6f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(starTexture, starPosition, null, Style.StarSecond with { A = 0 }, Projectile.rotation * 0.1f, starOrigin, Projectile.scale * 0.4f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(starTexture, starPosition, null, Style.StarFirst with { A = 0 }, Projectile.rotation * 0.1f, starOrigin, Projectile.scale * 0.35f, SpriteEffects.None, 0f);
        }
    }

    public sealed class TheStellarThrowHitProjectile : ModProjectile, IInitializableProjectile, IPreDrawPixelatedProjectile
    {
        public static readonly int InitTimeLeft = GeneralUtils.SecondsToTicks(0.33f);

        private static readonly EasingBuilder _scaleEasing = new(
            (EasingFunctions.InOutExpo, 0.2f, 0f, 1f),
            (EasingFunctions.InOutQuad, 0.8f, 1f, 0f)
        );

        public override string Texture { get => TheStellarThrowAssets.InvisiblePath; }
        public float LifeTimeRatio { get => 1f - Projectile.timeLeft / (float)InitTimeLeft; }
        private int StyleIndex { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private ref TheStellarThrowPalette Style { get => ref TheStellarThrowPalette.Palettes[StyleIndex]; }

        public override void SetDefaults()
        {
            Projectile.DefaultToVisualEffect();

            Projectile.timeLeft = InitTimeLeft;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        void IInitializableProjectile.Initialize(Projectile _)
        {
            for (int i = 0; i < 14; i++)
            {
                var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));

                if (Main.rand.NextBool(3))
                {
                    var particle = WorldParticleManager.SpawnParticle<StarParticle>();

                    particle.LifeTime = GeneralUtils.SecondsToTicks(0.5f);
                    particle.Position = Projectile.Center + vector * Projectile.width * Main.rand.NextFloat() * 2f;
                    particle.Velocity = vector;
                    particle.StartColor = Style.StarFirst;
                    particle.EndColor = Style.StarSecond;
                    particle.Scale = Main.rand.NextFloat(1.0f, 2.0f);
                }
                else
                {
                    var particle = WorldParticleManager.SpawnParticle<LightPointParticle>();

                    particle.LifeTime = GeneralUtils.SecondsToTicks(0.5f);
                    particle.Position = Projectile.Center + vector * Projectile.width * Main.rand.NextFloat() * 2f;
                    particle.Velocity = vector;
                    particle.StartColor = Style.StarFirst;
                    particle.EndColor = Style.StarSecond;
                    particle.Scale = Main.rand.NextFloat(0.3f, 0.6f);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                var particle = WorldParticleManager.SpawnParticle<SmokeParticle>(WorldParticleFlags.Pixelated | WorldParticleFlags.Behind);

                particle.LifeTime = GeneralUtils.SecondsToTicks(1f);
                particle.Position = Projectile.Center + vector * Main.rand.NextFloat(TileUtils.TileSizeInPixels);
                particle.Velocity = vector * Main.rand.NextFloat(0.2f, 2f);
                particle.StartColor = new(new Color(100, 25, 75) * 0.25f, true);
                particle.EndColor = new(new Color(0, 0, 0, 0), false);
                particle.Scale = 2f;
            }
        }

        public override void AI()
        {
            Projectile.rotation += 0.3f;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        void IPreDrawPixelatedProjectile.PreDrawPixelated(Projectile _)
        {
            var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;

            var starTexture = TheStellarThrowAssets.StarTexture.Value;
            var starOrigin = starTexture.Size() * 0.5f;
            var starScale = _scaleEasing.Evaluate(LifeTimeRatio);

            Main.spriteBatch.Draw(starTexture, position, null, new Color(100, 25, 75) * 0.25f, Projectile.rotation * 0.05f, starOrigin, starScale * 0.8f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(starTexture, position, null, new Color(255, 0, 80) with { A = 0 }, Projectile.rotation * 0.1f, starOrigin, starScale * 0.6f, SpriteEffects.None, 0f);

            var circleTexture = TheStellarThrowAssets.CircleTexture.Value;
            var circleOrigin = circleTexture.Size() * 0.5f;
            var circleColor = new Color(255, 0, 80) with { A = 0 } * (1f - EasingFunctions.InOutQuart(LifeTimeRatio));

            Main.spriteBatch.Draw(circleTexture, position, null, circleColor, 0f, circleOrigin, LifeTimeRatio * 2f, SpriteEffects.None, 0f);
        }
    }
}
