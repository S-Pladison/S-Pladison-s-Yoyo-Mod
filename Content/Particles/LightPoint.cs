using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Utils;
using System;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Particles
{
    public sealed class LightPointParticle : BaseParticle
    {
        private static Asset<Texture2D> _texture;
        private static EasingBuilder _scaleEasing;

        private static Rectangle _firstFrameRect = new(1, 1, 32, 32);
        private static Rectangle _secondFrameRect = new(35, 1, 32, 32);
        private static Vector2 _origin = new(16, 16);

        public Color StartColor = Color.White;
        public Color EndColor = Color.White;

        private float _innerScale = 1.0f;

        public float Scale { get => _innerScale; set => _innerScale = Math.Max(value, 0.0f); }

        public override void Load(Mod mod)
        {
            _texture = ModContent.Request<Texture2D>($"{nameof(SPYoyoMod)}/Assets/Particles/{nameof(LightPointParticle)}");
            _scaleEasing = new(
                (EasingFunctions.InOutQuart, 0.1f, 0f, 1f),
                (EasingFunctions.Linear, 0.9f, 1f, 0f)
            );
        }

        public override void Unload()
        {
            _texture = null;
            _scaleEasing = null;
        }

        protected override void OnUpdate()
        {
            Velocity *= 0.96f;
        }

        public override void Draw(SpriteBatch spriteBatch, in Vector2 screenPosition)
        {
            var position = Position - screenPosition;
            var color = Color.Lerp(StartColor, EndColor, LifeTimeRatio);
            var scale = Scale * _scaleEasing.Evaluate(LifeTimeRatio) * 0.8f;

            spriteBatch.Draw(_texture.Value, position, _secondFrameRect, Color.Black * 0.5f, 0f, _origin, scale * 1.2f, SpriteEffects.None, 0f);
            spriteBatch.Draw(_texture.Value, position, _firstFrameRect, color with { A = 0 }, 0f, _origin, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(_texture.Value, position, _firstFrameRect, Color.White with { A = 0 } * 0.75f, 0f, _origin, scale * 0.33f, SpriteEffects.None, 0f);
        }
    }
}