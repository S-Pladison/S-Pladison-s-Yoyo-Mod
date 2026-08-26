using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System;
using Terraria;

namespace SPYoyoMod.Content.Particles
{
    public sealed class RotatingCubeParticle : BaseParticle
    {
        public static readonly LazyAsset<Texture2D> Texture = LazyAsset<Texture2D>.From($"{nameof(SPYoyoMod)}/Assets/Particles/{nameof(RotatingCubeParticle)}");

        private const int FrameCount = 4;
        private const int FrameSize = 32;
        private const int FrameDuration = 5;

        private static readonly Vector2 _origin = new(FrameSize * 0.5f, FrameSize * 0.5f);
        private static readonly EasingBuilder _fadeEasing = new(
            (EasingFunctions.OutCubic, 0.2f, 0f, 1f),
            (EasingFunctions.Linear, 0.6f, 1f, 1f),
            (EasingFunctions.InCubic, 0.2f, 1f, 0f)
        );

        public Color StartColor = Color.White;
        public Color EndColor = Color.White;

        private readonly int _startFrame = Main.rand.Next(FrameCount);
        private readonly int _turnDirection = Main.rand.NextBool() ? 1 : -1;
        private float _innerScale = 1.0f;

        public float Scale { get => _innerScale; set => _innerScale = Math.Max(value, 0.0f); }

        protected override void OnUpdate()
        {
            Velocity *= 0.96f;

            var fade = _fadeEasing.Evaluate(LifeTimeRatio);
            var color = Color.Lerp(StartColor, EndColor, LifeTimeRatio) with { A = (byte)(255f * fade) };

            Lighting.AddLight(Position, color.ToVector3() * 0.1f);
        }

        public override void Draw(SpriteBatch spriteBatch, in Vector2 screenPosition)
        {
            var position = Position - screenPosition;
            var fade = _fadeEasing.Evaluate(LifeTimeRatio);
            var color = Color.Lerp(StartColor, EndColor, LifeTimeRatio) with { A = (byte)(255f * fade) };
            var frameIndex = ((_startFrame + _turnDirection * (ElapsedTime / FrameDuration)) % FrameCount + FrameCount) % FrameCount;
            var source = new Rectangle(frameIndex * FrameSize, 0, FrameSize, FrameSize);

            spriteBatch.Draw(Texture.Value, position, source, color, 0f, _origin, Scale * fade, SpriteEffects.None, 0f);
        }
    }
}
