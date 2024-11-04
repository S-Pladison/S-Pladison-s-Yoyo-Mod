using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    public sealed class BellowingThunderAssets : ILoadable
    {
        public const string ItemPath = $"{_path}BellowingThunder_Item";
        public const string ProjPath = $"{_path}BellowingThunder_Proj";
        public const string StringPath = $"{nameof(SPYoyoMod)}/Assets/FishingLine_WithShadow";

        public static Asset<Texture2D> GlowTexture { get; private set; } = ModContent.Request<Texture2D>($"{nameof(SPYoyoMod)}/Assets/YoyoGlow_WithShadow");

        private const string _path = $"{nameof(SPYoyoMod)}/Assets/Items/Mod.Yoyos/BellowingThunder/";

        void ILoadable.Unload()
        {
            GlowTexture = null;
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

    public sealed class BellowingThunderProjectile : YoyoBaseProjectile, IInitializableProjectile
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
    }
}