using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System;

namespace SPYoyoMod.Content.Particles
{
    public sealed class LightPointParticle : BaseParticle
    {
        public static readonly LazyAsset<Texture2D> Texture = LazyAsset<Texture2D>.From($"{nameof(SPYoyoMod)}/Assets/Particles/{nameof(LightPointParticle)}");

        private static readonly Rectangle _firstFrameRect = new(1, 1, 32, 32);
        private static readonly Rectangle _secondFrameRect = new(35, 1, 32, 32);
        private static readonly Vector2 _origin = new(16, 16);
        private static readonly EasingBuilder _scaleEasing = new(
            (EasingFunctions.InOutQuart, 0.1f, 0f, 1f),
            (EasingFunctions.Linear, 0.9f, 1f, 0f)
        );

        public Color StartColor = Color.White;
        public Color EndColor = Color.White;

        private float _innerScale = 1.0f;

        public float Scale { get => _innerScale; set => _innerScale = Math.Max(value, 0.0f); }

        protected override void OnUpdate()
        {
            Velocity *= 0.96f;
        }

        public override void Draw(SpriteBatch spriteBatch, in Vector2 screenPosition)
        {
            var position = Position - screenPosition;
            var color = Color.Lerp(StartColor, EndColor, LifeTimeRatio);
            var scale = Scale * _scaleEasing.Evaluate(LifeTimeRatio) * 0.8f;

            spriteBatch.Draw(Texture.Value, position, _secondFrameRect, Color.Black * 0.5f, 0f, _origin, scale * 1.2f, SpriteEffects.None, 0f);
            spriteBatch.Draw(Texture.Value, position, _firstFrameRect, color with { A = 0 }, 0f, _origin, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(Texture.Value, position, _firstFrameRect, Color.White with { A = 0 } * 0.75f, 0f, _origin, scale * 0.33f, SpriteEffects.None, 0f);
        }
    }
}