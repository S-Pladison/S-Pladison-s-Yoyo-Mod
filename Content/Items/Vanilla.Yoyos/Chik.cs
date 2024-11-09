using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class ChikAssets : ILoadable
    {
        public const string StringPath = $"{_assetPath}FishingLine_WithShadow";

        public static Asset<Texture2D> GlowTexture { get; private set; } = ModContent.Request<Texture2D>($"{_assetPath}YoyoGlow_WithShadow");

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Chik/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class ChikItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Chik;
    }

    public sealed class ChikProjectile : VanillaYoyoBaseProjectile, IInitializableProjectile
    {
        public static readonly Color GlowColor = new(55, 160, 255);

        private YoyoStringRenderer _stringRenderer;

        public override int ProjType => ProjectileID.Chik;
        public override bool InstancePerEntity => true;

        void IInitializableProjectile.Initialize(Projectile _)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _stringRenderer = new YoyoStringRenderer(new IDrawYoyoStringSegments.Gradient(
               ModContent.Request<Texture2D>(ChikAssets.StringPath, AssetRequestMode.ImmediateLoad).Value,
               (Color.Transparent, true), (Color.Transparent, true), (GlowColor, true)
            ));
        }

        public override void AI(Projectile proj)
        {
            proj.rotation -= 0.15f;

            Lighting.AddLight(proj.Center, GlowColor.ToVector3() * 0.2f);
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            if (_stringRenderer is null)
                return;

            var settings = new YoyoStringRendererSettings(
                proj: proj,
                start: mountedCenter + proj.GetOwner()?.gfxOffY * Vector2.UnitY ?? Vector2.Zero,
                offset: -Main.screenPosition
            );

            _stringRenderer.Render(Main.spriteBatch, settings);
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            var glowPosition = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var glowTexture = ChikAssets.GlowTexture.Value;
            var glowOrigin = glowTexture.Size() * 0.5f;
            var glowScale = proj.scale * 1.2f;

            Main.spriteBatch.Draw(glowTexture, glowPosition, null, GlowColor, proj.rotation, glowOrigin, glowScale, SpriteEffects.None, 0f);

            return true;
        }
    }
}