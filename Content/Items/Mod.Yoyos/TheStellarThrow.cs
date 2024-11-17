using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Content.Items.Vanilla.Yoyos;
using SPYoyoMod.Content.Particles;
using SPYoyoMod.Core.Graphics;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class TheStellarThrowAssets : ILoadable
    {
        public const string ItemPath = $"{_yoyoPath}TheStellarThrow_Item";
        public const string ProjPath = $"{_yoyoPath}TheStellarThrow_Proj";

        public static Asset<Texture2D> GlowTexture { get; private set; } = ModContent.Request<Texture2D>($"{_assetPath}YoyoGlow_WithShadow");
        public static Asset<Texture2D> StarTexture { get; private set; } = ModContent.Request<Texture2D>($"{_yoyoPath}TheStellarThrow_Star");
        public static Asset<Texture2D> FlameTexture { get; private set; } = ModContent.Request<Texture2D>($"{_yoyoPath}TheStellarThrow_Flame");
        public static Asset<Effect> TrailEffect { get; private set; } = ModContent.Request<Effect>($"{_yoyoPath}TheStellarThrow_Trail");

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Mod.Yoyos/TheStellarThrow/";

        void ILoadable.Unload()
        {
            GlowTexture = null;
            StarTexture = null;
            FlameTexture = null;
            TrailEffect = null;
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
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

    public sealed class TheStellarThrowProjectile : YoyoBaseProjectile, IInitializableProjectile, IPreDrawPixelatedProjectile
    {
        public static readonly float SpawnStarRadius = TileUtils.TileSizeInPixels * 15f;
        public static readonly int SpawnStarCooldownMin = ModUtils.SecondsToTicks(1.5f);
        public static readonly int SpawnStarCooldownMax = ModUtils.SecondsToTicks(2f);
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
               ModContent.Request<Texture2D>(CascadeAssets.StringPath, AssetRequestMode.ImmediateLoad).Value,
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
            var starVelosity = Vector2.Normalize(target.Center - starPosition) * 24f;

            // TODO: Спавн звезды
            // TODO2: Добавить пасхалку; Если йо-йо выделен как избранный, то спавнятся золотые звезды, а не звезды другого цвета

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

                    particle.LifeTime = ModUtils.SecondsToTicks(0.5f);
                    particle.Position = Projectile.Center + Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Projectile.width * Main.rand.NextFloat() * 0.5f;
                    particle.StartColor = new Color(255, 175, 65);
                    particle.EndColor = new Color(255, 85, 225);
                    particle.Scale = Main.rand.NextFloat(0.8f, 1.0f);
                }
                else
                {
                    var particle = WorldParticleManager.SpawnParticle<LightPointParticle>();

                    particle.LifeTime = ModUtils.SecondsToTicks(0.5f);
                    particle.Position = Projectile.Center + Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * Projectile.width * Main.rand.NextFloat() * 0.5f;
                    particle.StartColor = new Color(255, 50, 160);
                    particle.EndColor = new Color(50, 50, 255);
                    particle.Scale = Main.rand.NextFloat(0.3f, 0.4f);
                }
            }

            Projectile.rotation -= 0.15f;

            Lighting.AddLight(Projectile.Center, StarColor.ToVector3() * 0.2f);
        }

        private void SetCooldownForStarSpawn(int? cooldown = null)
        {
            _cooldownTimer = cooldown ?? Main.rand.Next(SpawnStarCooldownMin, SpawnStarCooldownMax);
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
                        parameters["Repeats"].SetValue(_trailRenderer.Points.Distance() / CascadeAssets.FlameTexture.Width() / 128.0f / 4.0f);
                        parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                    })
                    .Apply();

                _trailRenderer.Render();
            }

            var starTexture = TheStellarThrowAssets.StarTexture;
            var starPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var starOrigin = starTexture.Size() * 0.5f;
            var starColor = new Color(100, 25, 75) * 0.35f;

            Main.spriteBatch.Draw(starTexture.Value, starPosition, null, starColor, Projectile.rotation * 0.05f, starOrigin, proj.scale * 0.6f, SpriteEffects.None, 0f);

            starColor = StarColor with { A = 0 };

            Main.spriteBatch.Draw(starTexture.Value, starPosition, null, starColor, Projectile.rotation * 0.1f, starOrigin, proj.scale * 0.4f, SpriteEffects.None, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var glowPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var glowTexture = CascadeAssets.GlowTexture.Value;
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
}
