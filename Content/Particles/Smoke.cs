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

        private static Vector2 _origin = new(32, 32);

        public Tuple<Color, bool> StartColor = new(Color.Gray, false);
        public Tuple<Color, bool> EndColor = new(Color.Black, false);

        private float _innerScale = 1.0f;
        private float _rotateDirection = Main.rand.NextBool() ? 1f : -1f;
        private float _rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        private Rectangle _frame = new(0, Main.rand.Next(4) * 64, 64, 64);

        public float Scale { get => _innerScale; set => _innerScale = Math.Max(value, 0.0f); }

        public override void Load(Mod mod)
        {
            _texture = ModContent.Request<Texture2D>($"{nameof(SPYoyoMod)}/Assets/Particles/{nameof(SmokeParticle)}");
        }

        public override void Unload()
        {
            _texture = null;
        }

        protected override void OnUpdate()
        {
            Velocity *= 0.96f;
            _rotation += _rotateDirection * 0.01f;
        }

        public override void Draw(SpriteBatch spriteBatch, in Vector2 screenPosition)
        {
            var position = Position - screenPosition;
            var scale = Scale * EasingFunctions.OutExpo(LifeTimeRatio);
            var startColor = StartColor.Item2 ? StartColor.Item1 : Lighting.GetColor(Position.ToTileCoordinates(), StartColor.Item1) with { A = StartColor.Item1.A };
            var endColor = EndColor.Item2 ? EndColor.Item1 : Lighting.GetColor(Position.ToTileCoordinates(), EndColor.Item1) with { A = EndColor.Item1.A };
            var color = Color.Lerp(startColor, endColor, LifeTimeRatio);

            spriteBatch.Draw(_texture.Value, position, _frame, color, _rotation, _origin, scale, SpriteEffects.None, 0f);
        }
    }
}
