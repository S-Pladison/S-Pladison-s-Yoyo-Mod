using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Yoyos;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class ValorAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string StringPath = $"{AssetPath}/FishingLine_WithShadow";

        public static readonly LazyAsset<Texture2D> GlowTexture = LazyAsset<Texture2D>.From($"{AssetPath}/YoyoGlow_WithShadow");
        public static readonly LazyAsset<Effect> TrailEffect = LazyAsset<Effect>.From($"{AssetPath}/TrailEffect");
    }

    public sealed class ValorItem : YoyoItem<ValorProjectile>
    {
        public override int OverrideType => ItemID.Valor;
    }

    public sealed class ValorProjectile : YoyoProjectile<ValorItem>, IInitializableProjectile, IEmitLightEntity, IPreDrawPixelatedProjectile
    {
        public override int OverrideType => ProjectileID.Valor;

        //=/-

        public static readonly Color GlowColor = new(35, 90, 255);
        public static readonly int TrailPointCount = 7;

        private YoyoStringRenderer _stringRenderer;
        private StripRenderer _trailRenderer;
        private LinkedList<Vector2> _oldPositions;

        void IInitializableProjectile.Initialize(Projectile proj)
        {
            if (Main.dedServ)
                return;

            _stringRenderer = new YoyoStringRenderer(new IDrawYoyoStringSegments.Gradient(
               ModContent.Request<Texture2D>(ValorAssets.StringPath, AssetRequestMode.ImmediateLoad).Value,
               (Color.Transparent, true), (GlowColor, true)
            ));

            _trailRenderer = new StripRenderer(Main.graphics.GraphicsDevice, capacity: TrailPointCount)
            {
                StartWidth = 16,
                EndWidth = 8,
                StartColor = GlowColor,
                EndColor = Color.Transparent
            };

            _oldPositions = [];
        }

        public override void OnKill(Projectile proj, int timeLeft)
        {
            _trailRenderer?.Dispose();
        }

        public override void AI(Projectile proj)
        {
            if (_trailRenderer is not null)
            {
                _oldPositions.AddFirst(proj.Center + proj.velocity);

                while (_oldPositions.Count > TrailPointCount)
                    _oldPositions.RemoveLast();

                _trailRenderer.SetPoints(_oldPositions);
            }

            if (Main.rand.NextBool(3))
            {
                var dust = Main.dust[Dust.NewDust(proj.position, proj.width, proj.height, Main.rand.NextBool() ? DustID.DungeonWater : DustID.WaterCandle)];
                dust.noGravity = true;
                dust.noLightEmittence = true;
            }
        }

        void IEmitLightEntity.EmitLight(Entity entity)
        {
            Lighting.AddLight(entity.Center, GlowColor.ToVector3() * 0.2f);
        }

        void IPreDrawPixelatedProjectile.PreDrawPixelated(Projectile proj)
        {
            if (_trailRenderer is null)
                return;

            ValorAssets.TrailEffect
                .Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(TextureAssets.MagicPixel.Value);
                    parameters["TransformMatrix"].SetValue(GameMatrices.World * GameMatrices.Effect * GameMatrices.Projection);
                })
                .Apply("Simple");

            _trailRenderer.Render();
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            var glowPosition = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var glowTexture = ValorAssets.GlowTexture.Value;
            var glowOrigin = glowTexture.Size() * 0.5f;
            var glowScale = proj.scale * 1.2f;

            Main.spriteBatch.Draw(glowTexture, glowPosition, null, GlowColor, proj.rotation, glowOrigin, glowScale, SpriteEffects.None, 0f);

            return true;
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            if (_stringRenderer is null)
                return;

            _stringRenderer.Render(Main.spriteBatch, YoyoStringRendererContext.FromProjectile(proj, mountedCenter));
        }
    }
}
