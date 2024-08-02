using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Particles
{
    public sealed class LightPoint(int lifeTime, bool pixelated, LightPoint.DrawData drawData) : BaseParticle(lifeTime, pixelated), ILoadable
    {
        private static Asset<Texture2D> _texture;
        private static EasingBuilder _scaleEasing;

        void ILoadable.Load(Mod mod)
        {
            _texture = ModContent.Request<Texture2D>($"{nameof(SPYoyoMod)}/Assets/Particles/{nameof(LightPoint)}");
            _scaleEasing = new(
                (EasingFunctions.InOutQuart, 0.1f, 0f, 1f),
                (EasingFunctions.Linear, 0.9f, 1f, 0f)
            );
        }

        void ILoadable.Unload()
        {
            _texture = null;
            _scaleEasing = null;
        }

        // ...

        public struct DrawData(float scale, Color startColor, Color endColor)
        {
            public float Scale = scale;
            public Color StartColor = startColor;
            public Color EndColor = endColor;

            public DrawData(float scale, Color color) : this(scale, color, color) { }
            public DrawData(Color color) : this(1f, color, color) { }
            public DrawData() : this(1f, Color.White, Color.White) { }
        }

        // ...

        private DrawData _drawData = drawData;

        public LightPoint() : this(ModUtils.SecondsToTicks(1f), false, default) { }

        protected override void OnUpdate()
        {
            Velocity *= 0.96f;
        }

        public override void Draw(Vector2 screenPosition)
        {
            var lifeProgress = 1f - TimeLeft / (float)LifeTime;
            var position = Position - Main.screenPosition;
            var color = Color.Lerp(_drawData.StartColor, _drawData.EndColor, lifeProgress);
            var scale = _drawData.Scale * _scaleEasing.Evaluate(lifeProgress) * 0.8f;
            var origin = new Vector2(16, 16);

            Main.spriteBatch.Draw(_texture.Value, position, new Rectangle(35, 1, 32, 32), Color.Black * 0.5f, 0f, origin, scale * 1.2f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(_texture.Value, position, new Rectangle(1, 1, 32, 32), color with { A = 0 }, 0f, origin, scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(_texture.Value, position, new Rectangle(1, 1, 32, 32), Color.White with { A = 0 } * 0.75f, 0f, origin, scale * 0.33f, SpriteEffects.None, 0f);
        }
    }
}