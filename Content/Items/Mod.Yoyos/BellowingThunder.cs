using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Graphics.RenderTargets;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    public sealed class BellowingThunderAssets : ILoadable
    {
        public const string ItemPath = $"{_yoyoPath}BellowingThunder_Item";
        public const string ProjPath = $"{_yoyoPath}BellowingThunder_Proj";
        public const string StringPath = $"{_assetPath}FishingLine_WithShadow";

        public static Asset<Texture2D> LightningTexture { get; private set; } = ModContent.Request<Texture2D>($"{_yoyoPath}BellowingThunder_Lightning");
        public static Asset<Texture2D> GlowTexture { get; private set; } = ModContent.Request<Texture2D>($"{_assetPath}YoyoGlow_WithShadow");
        public static Asset<Effect> LightningEffect { get; private set; } = ModContent.Request<Effect>($"{_yoyoPath}BellowingThunderEffect_Lightning");

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Mod.Yoyos/BellowingThunder/";

        void ILoadable.Unload()
        {
            LightningTexture = null;
            GlowTexture = null;
            LightningEffect = null;
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class BellowingThunderItem : YoyoBaseItem
    {
        public override string Texture => BellowingThunderAssets.ItemPath;
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
    }

    public sealed class BellowingThunderProjectile : YoyoBaseProjectile, IInitializableProjectile, IPostDrawPixelatedProjectile
    {
        public static readonly Color GlowColor = new(208, 99, 219);

        private YoyoStringRenderer _stringRenderer;

        public override string Texture => BellowingThunderAssets.ProjPath;
        public override float LifeTime => -1f;
        public override float MaxRange => 235f;
        public override float TopSpeed => 14f;

        public void Initialize(Projectile _)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _stringRenderer = new YoyoStringRenderer(new IDrawYoyoStringSegments.Gradient(
                ModContent.Request<Texture2D>(BellowingThunderAssets.StringPath, AssetRequestMode.ImmediateLoad).Value,
                (Color.Transparent, true), (Color.Transparent, true), (GlowColor, true)
            ));

            ModContent.GetInstance<BellowingThunderLightningEffectHandler>().Add(Projectile);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            ModContent.GetInstance<BellowingThunderLightningEffectHandler>().Remove(Projectile);
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.2f);
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
            if (_stringRenderer is null)
                return;

            var settings = new YoyoStringRendererSettings(
                proj: Projectile,
                start: mountedCenter + Projectile.GetOwner()?.gfxOffY * Vector2.UnitY ?? Vector2.Zero,
                offset: -Main.screenPosition
            );

            _stringRenderer.Render(Main.spriteBatch, settings);
        }

        void IPostDrawPixelatedProjectile.PostDrawPixelated(Projectile proj)
        {
            var timeForVisualEffects = (float)Main.timeForVisualEffects + Projectile.whoAmI * 111f;

            var lightningPosition = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var lightningTexture = BellowingThunderAssets.LightningTexture.Value;
            var lightningOrigin = new Vector2(48, 48);
            var lightningFrameIndex = (int)((timeForVisualEffects * 0.2f) % 16);
            var lightningFrame = new Rectangle(96 * (lightningFrameIndex / 4), 96 * (lightningFrameIndex % 4), 96, 96);
            var lightninRotation = ((int)(timeForVisualEffects * 0.2f) / 16) * MathHelper.PiOver2;

            Main.spriteBatch.Draw(lightningTexture, lightningPosition, lightningFrame, Color.White with { A = 0 }, lightninRotation, lightningOrigin, 0.4f, SpriteEffects.None, 0f);
        }
    }

    public interface IDrawBellowingThunderLightning
    {
        void DrawLightning();
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class BellowingThunderLightningEffectHandler : ILoadable
    {
        private readonly ScreenRenderTarget _renderTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.TwiceSmaller);
        private readonly ProjectileObserver _projObserver = new(p => p.ModProjectile is not IDrawBellowingThunderLightning);

        private bool _targetWasPrepared = false;

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
            ModEvents.OnPostUpdateEverything += _projObserver.Update;
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
            ModEvents.OnPostUpdateEverything -= _projObserver.Update;
        }

        private void DrawToTarget()
        {
            if (!_projObserver.AnyEntity)
                return;

            _targetWasPrepared = false;

            var spriteBatchSpanshot = new SpriteBatchSnapshot
            {
                SortMode = SpriteSortMode.Deferred,
                BlendState = BlendState.Additive,
                SamplerState = Main.DefaultSamplerState,
                DepthStencilState = DepthStencilState.None,
                RasterizerState = Main.Rasterizer,
                Effect = null,
                Matrix = Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix
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
                    (proj.ModProjectile as IDrawBellowingThunderLightning).DrawLightning();
                }
                Main.spriteBatch.End();
            }
            device.SetRenderTarget(null);

            _targetWasPrepared = true;
        }

        private void DrawTargetToScreen()
        {
            if (!_targetWasPrepared)
                return;

            var effect = BellowingThunderAssets.LightningEffect;

            effect.Prepare(parameters =>
            {
                parameters["ScreenSize"].SetValue(_renderTarget.Size);
                parameters["Color"].SetValue(new Color(145, 60, 195).ToVector4());
            });

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, effect.Value, Main.GameViewMatrix.ZoomMatrix);
            Main.spriteBatch.Draw(_renderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }
    }
}