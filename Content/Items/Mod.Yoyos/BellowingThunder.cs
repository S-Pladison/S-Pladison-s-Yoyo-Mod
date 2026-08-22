using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Core;
using SPYoyoMod.Core.Graphics;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Graphics.RenderTargets;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using SPYoyoMod.Utils.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    public sealed class BellowingThunderAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Mod.Yoyos/BellowingThunder/BellowingThunder";

        public const string ItemPath = $"{YoyoPath}_Item";
        public const string ProjPath = $"{YoyoPath}_Proj";
        public const string InvisiblePath = $"{AssetPath}/Invisible";
        public const string StringPath = $"{AssetPath}/FishingLine_WithShadow";

        public static readonly LazyAsset<Texture2D> ElectricityTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Electricity");
        public static readonly LazyAsset<Texture2D> GlowTexture = LazyAsset<Texture2D>.From($"{AssetPath}/YoyoGlow_WithShadow");
        public static readonly LazyAsset<Texture2D> CircleTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Circle");
        public static readonly LazyAsset<Texture2D> StarTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Star");
        public static readonly LazyAsset<Texture2D> LightningTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Lightning");
        public static readonly LazyAsset<Effect> TrailEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Trail");
        public static readonly LazyAsset<Effect> LightningEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Lightning");
        public static readonly LazyAsset<Effect> ScreenEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Screen");
        public static readonly LazyAsset<Effect> SilhouetteEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Silhouette");
        public static readonly SoundStyle LightningStrikeSound = new($"{YoyoPath}Sound_LightningStrike");
    }

    public sealed class BellowingThunderItem : YoyoBaseItem
    {
        public const int StormCritBonus = 6;

        public override string Texture => BellowingThunderAssets.ItemPath;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(StormCritBonus);
        public override int GamepadExtraRange => 10;

        public override void SetDefaults()
        {
            base.SetDefaults();

            Item.damage = 27;
            Item.knockBack = 3.5f;
            Item.crit = 6;

            Item.shoot = ModContent.ProjectileType<BellowingThunderProjectile>();

            Item.rare = ItemRarityID.Orange;
            Item.value = ItemUtils.SellPrice(platinum: 0, gold: 4, silver: 0, copper: 0);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var critLine = tooltips.Find(VanillaTooltipLine.CritChance);

            if (critLine is null)
                return;

            ItemUtils.ModifyFirstIntegerInLine(critLine, (crit) =>
            {
                return crit + (Main.IsItStorming ? StormCritBonus : 0);
            });
        }
    }

    public sealed class BellowingThunderProjectile : YoyoBaseProjectile, IInitializableProjectile, IDrawPixelatedProjectile, IEmitLightEntity
    {
        public static readonly Color GlowColor = new(208, 99, 219);
        public static readonly int TrailPointCount = 5;
        public static readonly int ShadowTrailPointCount = TrailPointCount + 3;

        private int _initCritChance;
        private YoyoStringRenderer _stringRenderer;
        private StripRenderer _trailRenderer;
        private StripRenderer _shadowTrailRenderer;
        private LinkedList<Vector2> _oldPositions;

        public override string Texture => BellowingThunderAssets.ProjPath;
        public override float LifeTime => -1f;
        public override float MaxRange => 235f;
        public override float TopSpeed => 14f;

        public void Initialize(Projectile _)
        {
            _initCritChance = Projectile.CritChance;

            if (Main.dedServ)
                return;

            _stringRenderer = new YoyoStringRenderer(new IDrawYoyoStringSegments.Gradient(
                ModContent.Request<Texture2D>(BellowingThunderAssets.StringPath, AssetRequestMode.ImmediateLoad).Value,
                (Color.Transparent, true), (Color.Transparent, true), (GlowColor, true)
            ));

            _trailRenderer = new StripRenderer(Main.graphics.GraphicsDevice, capacity: TrailPointCount)
            {
                StartWidth = 4.25f,
                EndWidth = 0,
                StartColor = GlowColor,
                EndColor = GlowColor
            };

            _shadowTrailRenderer = new StripRenderer(Main.graphics.GraphicsDevice, capacity: ShadowTrailPointCount)
            {
                StartWidth = 6.5f,
                EndWidth = 0,
                StartColor = Color.Black * 0.25f,
                EndColor = Color.Black * 0.15f
            };

            _oldPositions = [];

            ModContent.GetInstance<BellowingThunderScreenEffectHandler>().Add(Projectile);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            _trailRenderer?.Dispose();
            _shadowTrailRenderer?.Dispose();

            ModContent.GetInstance<BellowingThunderScreenEffectHandler>().Remove(Projectile);
        }

        public override void AI()
        {
            Projectile.CritChance = _initCritChance + (Main.IsItStorming ? BellowingThunderItem.StormCritBonus : 0);

            if (_trailRenderer is not null) //< Если он не null, то и _shadowTrailRenderer тоже
            {
                _oldPositions.AddFirst(Projectile.Center + Projectile.velocity);

                while (_oldPositions.Count > ShadowTrailPointCount)
                    _oldPositions.RemoveLast();

                _trailRenderer.SetPoints(_oldPositions.Take(TrailPointCount).ToArray());
                _shadowTrailRenderer.SetPoints(_oldPositions);
            }

            if (Main.rand.NextBool(7))
            {
                var dust = Main.dust[Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.VenomStaff)];
                dust.noGravity = true;
                dust.noLightEmittence = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!hit.Crit || Projectile.ai[0] == -1 || Projectile.GetOwner().OwnedProjectileCounts<BellowingThunderRingProjectile>() != 0) //< Вторая проверка - возвращается ли йо-йо к игроку
                return;

            var source = Projectile.GetSource_OnHit(target);
            var projType = ModContent.ProjectileType<BellowingThunderRingProjectile>();
            var ringProjIndex = Projectile.NewProjectile(source, Projectile.Center, Vector2.Zero, projType, Projectile.damage, Projectile.knockBack, Projectile.owner);

            Main.projectile[ringProjIndex].CritChance = _initCritChance;
        }

        void IEmitLightEntity.EmitLight(Entity _)
        {
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.2f);
        }

        void IPreDrawPixelatedProjectile.PreDrawPixelated(Projectile _)
        {
            BellowingThunderAssets.TrailEffect
                .Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(TextureAssets.MagicPixel.Value);
                    parameters["TransformMatrix"].SetValue(GameMatrices.World * GameMatrices.Effect * GameMatrices.Projection);
                })
                .Apply();

            _shadowTrailRenderer.Render();
            _trailRenderer.Render();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var glowPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var glowTexture = BellowingThunderAssets.GlowTexture.Value;
            var glowOrigin = glowTexture.Size() * 0.5f;
            var glowScale = Projectile.scale * 1.2f;

            Main.spriteBatch.Draw(glowTexture, glowPosition, null, GlowColor, Projectile.rotation, glowOrigin, glowScale, SpriteEffects.None, 0f);

            return true;
        }

        public override void PostDrawYoyoString(Vector2 mountedCenter)
        {
            _stringRenderer.Render(Main.spriteBatch, YoyoStringRendererContext.FromProjectile(Projectile, mountedCenter));
        }

        void IPostDrawPixelatedProjectile.PostDrawPixelated(Projectile _)
        {
            var timeForVisualEffects = (float)Main.timeForVisualEffects + Projectile.whoAmI * 111f;

            var electricityPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var electricityTexture = BellowingThunderAssets.ElectricityTexture.Value;
            var electricityOrigin = new Vector2(48, 48);
            var electricityFrameIndex = (int)((timeForVisualEffects * 0.2f) % 16);
            var electricityFrame = new Rectangle(96 * (electricityFrameIndex / 4), 96 * (electricityFrameIndex % 4), 96, 96);
            var electricityRotation = ((int)(timeForVisualEffects * 0.2f) / 16) * MathHelper.PiOver2;

            Main.spriteBatch.Draw(electricityTexture, electricityPosition, electricityFrame, Color.White with { A = 0 }, electricityRotation, electricityOrigin, 0.4f, SpriteEffects.None, 0f);
        }
    }

    public sealed class BellowingThunderRingProjectile : ModProjectile, IInitializableProjectile, IEmitLightEntity
    {
        public static readonly int MaxRadius = TileUtils.TileSizeInPixels * 4;
        public static readonly int InitTimeLeft = GeneralUtils.SecondsToTicks(3f);

        private static readonly EasingBuilder _lightningStrikeWidthEasing = new(
            (EasingFunctions.InOutCubic, 0.05f, 0f, 1f),
            (EasingFunctions.Linear, 0.05f, 1f, 1f),
            (EasingFunctions.InOutCubic, 0.05f, 1f, 0f),
            (EasingFunctions.Linear, 0.85f, 0f, 0f)
        );

        private static readonly EasingBuilder _starEasing = new(
            (EasingFunctions.InOutCubic, 0.05f, 0f, 1f),
            (EasingFunctions.Linear, 0.75f, 1f, 1f),
            (EasingFunctions.InOutCubic, 0.2f, 1f, 0f)
        );

        private static readonly EasingBuilder _ringRadiusEasing = new(
            (EasingFunctions.OutBack, 0.05f, 0f, 1f),
            (EasingFunctions.InExpo, 0.80f, 1f, 0.8f),
            (EasingFunctions.Linear, 0.15f, 0.8f, 0f)
        );

        private int _initCritChance;
        private StripRenderer _stripRenderer;
        private RingRenderer _ringRenderer;

        public override string Texture => BellowingThunderAssets.InvisiblePath;
        public float LifeTimeRatio => 1f - Projectile.timeLeft / (float)InitTimeLeft;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = MaxRadius * 2;
            Projectile.height = MaxRadius * 2;

            Projectile.timeLeft = InitTimeLeft;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;

            Projectile.netImportant = true;
        }

        void IInitializableProjectile.Initialize(Projectile _)
        {
            _initCritChance = Projectile.CritChance;

            if (Main.dedServ)
                return;

            Projectile.soundDelay = 12;

            _stripRenderer = new StripRenderer(Main.graphics.GraphicsDevice, 2);
            _ringRenderer = new RingRenderer(Main.graphics.GraphicsDevice, 25);

            ModContent.GetInstance<BellowingThunderScreenEffectHandler>().Add(Projectile);

            ScreenEffectManager.Punch(new ScreenEffectManager.PunchSettings()
            {
                Position = Projectile.Center,
                Direction = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)),
                Strength = 7f,
                VibrationCyclesPerSecond = 6f,
                Frames = 15,
                DistanceFalloff = 16f * 25f
            });

            SoundEngine.PlaySound(BellowingThunderAssets.LightningStrikeSound, Projectile.Center);

            if (!Projectile.IsLocalPlayerAsOwner())
                return;

            ScreenEffectManager.Flash(new ScreenEffectManager.FlashSettings()
            {
                Strength = 0.15f,
                Frames = 25,
                Position = Projectile.Center
            });

            ModContent.GetInstance<BellowingThunderSilhouetteEffectHandler>().Activate();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            _stripRenderer?.Dispose();
            _ringRenderer?.Dispose();

            ModContent.GetInstance<BellowingThunderScreenEffectHandler>().Remove(Projectile);
        }

        public override void AI()
        {
            Projectile.CritChance = _initCritChance + (Main.IsItStorming ? BellowingThunderItem.StormCritBonus : 0);

            var yoyoProj = Main.ActiveProjectiles.FirstOrDefault(p => p.type == ModContent.ProjectileType<BellowingThunderProjectile>() && p.owner == Projectile.owner && p.IsPrimaryYoyo());

            if (yoyoProj is null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = yoyoProj.Center;

            if (Projectile.soundDelay <= 0)
            {
                Projectile.soundDelay = 12;

                SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap with { PitchVariance = 0.4f }, Projectile.Center);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            var radius = MaxRadius * _ringRadiusEasing.Evaluate(LifeTimeRatio);

            return CollisionUtils.CheckRectanglevCircle(targetHitbox, projHitbox.Center.ToVector2(), radius);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.GetOwner().Counterweight(target.Center, Projectile.damage, Projectile.knockBack);
        }

        void IEmitLightEntity.EmitLight(Entity _)
        {
            Lighting.AddLight(Projectile.Center, new Color(208, 99, 219).ToVector3() * 0.4f * _ringRadiusEasing.Evaluate(LifeTimeRatio));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var circlePosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var circleTexture = BellowingThunderAssets.CircleTexture.Value;
            var circleColor = new Color(145, 60, 195) with { A = 0 } * EasingFunctions.InOutCubic(LifeTimeRatio) * 0.2f;
            var circleScale = MathHelper.Lerp(4f, 0f, EasingFunctions.InCubic(LifeTimeRatio));

            Main.spriteBatch.Draw(circleTexture, circlePosition, null, circleColor, 0f, circleTexture.Size() * 0.5f, circleScale, SpriteEffects.None, 0f);

            return true;
        }

        public void DrawLightning()
        {
            var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var ringThickness = TileUtils.TileSizeInPixels * 5f * _ringRadiusEasing.Evaluate(LifeTimeRatio);

            BellowingThunderAssets.LightningEffect
                .Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(BellowingThunderAssets.LightningTexture.Value);
                    parameters["TransformMatrix"].SetValue(GameMatrices.Effect * GameMatrices.Projection);
                    parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                    parameters["Repeats"].SetValue(3f);
                    parameters["Fade"].SetValue(false);
                })
                .Apply();

            _ringRenderer
                .SetThickness(ringThickness)
                .SetRadius(MaxRadius * _ringRadiusEasing.Evaluate(LifeTimeRatio) + ringThickness * 0.5f)
                .SetPointCount((int)MathHelper.Lerp(5, 25, _ringRadiusEasing.Evaluate(LifeTimeRatio)))
                .SetPosition(position)
                .Render();

            var lightningStartPosition = position - Vector2.UnitY * Main.screenHeight;
            var lightningEndPosition = position;

            BellowingThunderAssets.LightningEffect
                .Prepare(parameters =>
                {
                    parameters["Repeats"].SetValue(2f);
                    parameters["Fade"].SetValue(true);
                });

            _stripRenderer
                .SetWidth(TileUtils.TileSizeInPixels * 16 * _lightningStrikeWidthEasing.Evaluate(LifeTimeRatio))
                .SetPoints([lightningStartPosition, lightningEndPosition])
                .Render();

            var starTexture = BellowingThunderAssets.StarTexture.Value;
            var starRotation = EasingFunctions.InOutSine(LifeTimeRatio) * MathHelper.PiOver2;
            var starScale = _starEasing.Evaluate(LifeTimeRatio) * 2f;

            Main.spriteBatch.Draw(starTexture, position, null, Color.White, EasingFunctions.InOutSine(starRotation) * MathHelper.PiOver2, starTexture.Size() * 0.5f, starScale, SpriteEffects.None, 0f);
        }
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class BellowingThunderScreenEffectHandler : ILoadable
    {
        private readonly ScreenRenderTarget _renderTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.Half);
        private readonly ProjectileObserver _projObserver = ProjectileObserver.Create(p => p.ModProjectile is not BellowingThunderRingProjectile);

        public RenderTarget2D Target => _renderTarget;

        public void Add(Projectile proj)
        {
            _projObserver.Add(proj);
        }

        public void Remove(Projectile proj)
        {
            _projObserver.Remove(proj);
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            ModEvents.OnPostUpdateCameraPosition += DrawToTarget;
            ModEvents.OnWorldUnload += _projObserver.Clear;

            On_Main.DrawPlayers_AfterProjectiles += (orig, main) =>
            {
                orig(main);
                DrawTargetToScreen();
            };
        }

        void ILoadable.Unload()
        {
            ModEvents.OnWorldUnload -= _projObserver.Clear;
            ModEvents.OnPostUpdateCameraPosition -= DrawToTarget;
        }

        private void DrawToTarget()
        {
            if (!_projObserver.AnyEntity)
                return;

            var spriteBatchSpanshot = new SpriteBatchSnapshot
            {
                SortMode = SpriteSortMode.Deferred,
                BlendState = BlendState.Additive,
                SamplerState = Main.DefaultSamplerState,
                DepthStencilState = DepthStencilState.None,
                RasterizerState = Main.Rasterizer,
                Effect = null,
                Matrix = GameMatrices.Effect * Matrix.CreateScale(0.5f)
            };

            var device = Main.graphics.GraphicsDevice;

            device.BlendState = spriteBatchSpanshot.BlendState;
            device.SamplerStates[0] = spriteBatchSpanshot.SamplerState;
            device.DepthStencilState = spriteBatchSpanshot.DepthStencilState;
            device.RasterizerState = spriteBatchSpanshot.RasterizerState;

            device.SetRenderTarget(_renderTarget);
            device.Clear(Color.Transparent);
            {
                Main.spriteBatch.Begin(spriteBatchSpanshot);
                foreach (var proj in _projObserver.GetEntityInstances())
                {
                    proj.As<BellowingThunderRingProjectile>().DrawLightning();
                }
                Main.spriteBatch.End();
            }
            device.SetRenderTarget(null);

            // Костыль, исправляющий проблему с отрисовкой трейлов Зенита, Радужного жезла, да и скорее всего других модовых трейлов, если они рисуют их как ванилка...
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, GameMatrices.Transform);
            Main.spriteBatch.End();
        }

        private void DrawTargetToScreen()
        {
            if (!_projObserver.AnyEntity)
                return;

            var effect = BellowingThunderAssets.ScreenEffect.Prepare(parameters =>
            {
                parameters["ScreenSize"].SetValue(_renderTarget.Size);
                parameters["Color"].SetValue(new Color(145, 60, 195).ToVector4());
            });

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, effect.Value, GameMatrices.Zoom);
            Main.spriteBatch.Draw(_renderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }
    }

    [Autoload(Side = ModSide.Client), LoadAfter]
    public sealed class BellowingThunderSilhouetteEffectHandler : ILoadable
    {
        public static readonly string FilterName = $"{nameof(SPYoyoMod)}:BellowingThunderSilhouette";
        public static readonly int FilterActiveTime = GeneralUtils.SecondsToTicks(0.2f);

        private readonly ScreenRenderTarget _renderTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.Default);

        private int _filterTime;
        private Player _visualPlayer;

        public void Activate()
        {
            _filterTime = FilterActiveTime;
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            Filters.Scene[FilterName] = new Filter(
                new ScreenShaderData(BellowingThunderAssets.SilhouetteEffect, "BellowingThunderSilhouette"), EffectPriority.VeryHigh
            );

            ModEvents.OnPostUpdateEverything += UpdateFilterTimer;
            ModEvents.OnPostUpdateCameraPosition += UpdateFilterState;
        }

        void ILoadable.Unload()
        {
            ModEvents.OnPostUpdateCameraPosition -= UpdateFilterState;
            ModEvents.OnPostUpdateEverything -= UpdateFilterTimer;
        }

        private void UpdateFilterTimer()
        {
            _filterTime = Math.Max(_filterTime - 1, 0);
        }

        private void UpdateFilterState()
        {
            var filter = Filters.Scene[FilterName];

            if (_filterTime <= 0)
            {
                if (!filter.IsActive())
                    return;

                filter.GetShader().UseIntensity(0f);
                filter.Deactivate();

                return;
            }

            DrawToTarget();

            var shader = filter.GetShader();

            if (!filter.IsActive())
            {
                Filters.Scene.Activate(FilterName);
                shader.UseIntensity(1f);
            }

            shader.UseImage(_renderTarget);

            if (_filterTime < FilterActiveTime / 2)
            {
                shader.UseColor(Color.White);
                shader.UseSecondaryColor(Color.Black);
                return;
            }

            shader.UseColor(Color.Black);
            shader.UseSecondaryColor(Color.White);
        }

        private void DrawLocalPlayer()
        {
            var player = Main.LocalPlayer;

            _visualPlayer ??= new Player();
            _visualPlayer.CopyVisuals(player);
            _visualPlayer.isFirstFractalAfterImage = true;
            _visualPlayer.firstFractalAfterImageOpacity = 1f;
            _visualPlayer.ResetEffects();
            _visualPlayer.ResetVisibleAccessories();
            _visualPlayer.DisplayDollUpdate();
            _visualPlayer.itemRotation = player.itemRotation;
            _visualPlayer.gravDir = player.gravDir;
            _visualPlayer.heldProj = player.heldProj;
            _visualPlayer.Center = player.Center;
            _visualPlayer.wingFrame = player.wingFrame;
            _visualPlayer.velocity.Y = player.velocity.Y;
            _visualPlayer.socialIgnoreLight = true;

            var armRotation = player.itemRotation * player.gravDir - MathHelper.PiOver2 * player.direction;

            _visualPlayer.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            _visualPlayer.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, armRotation);
            _visualPlayer.PlayerFrame();

            Main.PlayerRenderer.DrawPlayer(Main.Camera, _visualPlayer, _visualPlayer.position, 0f, _visualPlayer.fullRotationOrigin, 0f);
        }

        private void DrawToTarget()
        {
            var lightningScreenTarget = ModContent.GetInstance<BellowingThunderScreenEffectHandler>().Target;

            if (lightningScreenTarget is null || lightningScreenTarget.IsDisposed)
                return;

            var device = Main.graphics.GraphicsDevice;
            device.SetRenderTarget(_renderTarget);
            device.Clear(Color.Transparent);
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(lightningScreenTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
                Main.spriteBatch.End();

                var playerRasterizerState = Main.GameViewMatrix.Effects.HasFlag(SpriteEffects.FlipHorizontally) ? RasterizerState.CullClockwise : RasterizerState.CullCounterClockwise;

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, playerRasterizerState, null, Main.GameViewMatrix.ZoomMatrix);
                DrawLocalPlayer();
                Main.spriteBatch.End();
            }
            device.SetRenderTarget(null);
        }
    }
}