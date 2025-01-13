using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class Code1Assets : ILoadable
    {
        public const string StringPath = $"{_assetPath}FishingLine_WithShadow";

        public static Asset<Texture2D> GlowTexture { get; private set; } = ModContent.Request<Texture2D>($"{_assetPath}YoyoGlow_WithShadow");

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Code1/";

        void ILoadable.Unload()
        {
            GlowTexture = null;
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class Code1Item : VanillaYoyoBaseItem
    {
        public const int CritBonus = 16;

        public override int ItemType => ItemID.Code1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritBonus);

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(item, tooltips); //< Не удалять!

            var critLine = tooltips.Find(VanillaTooltipLine.CritChance);

            if (critLine is null)
                return;

            ItemUtils.ModifyFirstIntegerInLine(critLine, static (crit) =>
            {
                return crit; //< TODO: Модифицировать, если рядом есть враги
            });
        }
    }

    public sealed class Code1Projectile : VanillaYoyoBaseProjectile, IInitializableProjectile, IEmitLightEntity
    {
        public static readonly Color GlowColor = new(65, 185, 255);

        private YoyoStringRenderer _stringRenderer;

        public override int ProjType => ProjectileID.Code1;
        public override bool InstancePerEntity => true;

        void IInitializableProjectile.Initialize(Projectile _)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _stringRenderer = new YoyoStringRenderer(new IDrawYoyoStringSegments.Gradient(
               ModContent.Request<Texture2D>(Code1Assets.StringPath, AssetRequestMode.ImmediateLoad).Value,
               (Color.Transparent, true), (GlowColor, true)
            ));
        }

        void IEmitLightEntity.EmitLight(Entity proj)
        {
            Lighting.AddLight(proj.Center, GlowColor.ToVector3() * 0.15f);
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            var glowPosition = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var glowTexture = Code1Assets.GlowTexture.Value;
            var glowOrigin = glowTexture.Size() * 0.5f;
            var glowScale = proj.scale * 1.2f;

            Main.spriteBatch.Draw(glowTexture, glowPosition, null, GlowColor, proj.rotation, glowOrigin, glowScale, SpriteEffects.None, 0f);

            return true;
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
    }
}