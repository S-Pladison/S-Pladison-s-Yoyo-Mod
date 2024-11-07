using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Utils;
using System;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Particles
{
    public sealed class SmokeParticle : BaseParticle
    {
        private static Asset<Texture2D> _texture;
        private static EasingBuilder _scaleEasing;

        private static Vector2 _origin = new(32, 32);

        public Tuple<Color, bool> StartColor = new(Color.Gray, false);
        public Tuple<Color, bool> EndColor = new(Color.Black, false);

        private float _innerScale = 1.0f;
        private float _rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        private Rectangle _frame = new(0, Main.rand.Next(4) * 64, 64, 64);

        public float Scale { get => _innerScale; set => _innerScale = Math.Max(value, 0.0f); }

        public override void Load(Mod mod)
        {
            _texture = ModContent.Request<Texture2D>($"{nameof(SPYoyoMod)}/Assets/Particles/{nameof(SmokeParticle)}");
            _scaleEasing = new(
                (EasingFunctions.OutQuart, 0.1f, 0f, 1f),
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
            var startColor = StartColor.Item2 ? StartColor.Item1 : Lighting.GetColor(Position.ToTileCoordinates(), StartColor.Item1);
            var endColor = EndColor.Item2 ? EndColor.Item1 : Lighting.GetColor(Position.ToTileCoordinates(), EndColor.Item1);
            var color = Color.Lerp(startColor, endColor, LifeTimeRatio) * (1f - LifeTimeRatio);
            var scale = Scale * _scaleEasing.Evaluate(LifeTimeRatio);

            spriteBatch.Draw(_texture.Value, position, _frame, color, _rotation, _origin, scale, SpriteEffects.None, 0f);
        }
    }
}
