using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.GameContent;

namespace SPYoyoMod.Common.Graphics
{
    public readonly struct YoyoStringSegment(Projectile proj, int index, Vector2 position, float rotation, float width)
    {
        public readonly Projectile Proj = proj;
        public readonly int Index = index;
        public readonly Vector2 Position = position;
        public readonly float Rotation = rotation;
        public readonly float Width = width;
    }

    public interface IDrawYoyoStringSegment
    {
        Texture2D Texture { get; }
        void Draw(int segmentCount, YoyoStringSegment segment);

        public record struct ColorData(Color Value, bool Glow)
        {
            public static implicit operator ColorData((Color Value, bool Glow) tuple)
                => new(tuple.Value, tuple.Glow);
        }

        public class Vanilla : IDrawYoyoStringSegment
        {
            public Texture2D Texture { get => TextureAssets.FishingLine.Value; }

            public void Draw(int segmentCount, YoyoStringSegment segment)
            {
                var rectangle = new Rectangle(0, 0, Texture.Width, (int)segment.Width);
                var origin = new Vector2(Texture.Width * 0.5f, 0f);
                var color = _tryApplyingPlayerStringColorFunc(segment.Proj.GetOwner().stringColor, Color.White with { A = (byte)(255 * 0.4f) });

                color = Lighting.GetColor(segment.Position.ToTileCoordinates(), color);
                color = new Color((byte)(color.R * 0.5f), (byte)(color.G * 0.5f), (byte)(color.B * 0.5f), (byte)(color.A * 0.5f));

                Main.spriteBatch.Draw(Texture, segment.Position - Main.screenPosition, rectangle, color, segment.Rotation, origin, 1f, SpriteEffects.None, 0f);
            }

            private static readonly TryApplyingPlayerStringColorDelegate _tryApplyingPlayerStringColorFunc = typeof(Main)
                ?.GetMethod("TryApplyingPlayerStringColor", BindingFlags.NonPublic | BindingFlags.Static)
                ?.CreateDelegate<TryApplyingPlayerStringColorDelegate>()
                ?? throw new InvalidOperationException($"Failed to init {nameof(TryApplyingPlayerStringColorDelegate)}");

            private delegate Color TryApplyingPlayerStringColorDelegate(int playerStringColor, Color defaultColor);
        }

        public class Default : IDrawYoyoStringSegment
        {
            public Texture2D Texture { get; init; }
            public ColorData Color { get; init; }

            public Default(ColorData color) : this(null, color) {}

            public Default(Texture2D texture, ColorData color)
            {
                Texture = texture ?? TextureAssets.FishingLine.Value;
                Color = color;
            }

            public void Draw(int _, YoyoStringSegment segment)
            {
                var rectangle = new Rectangle(0, 0, Texture.Width, (int)segment.Width);
                var origin = new Vector2(Texture.Width * 0.5f, 0f);
                var color = Color.Glow ? Color.Value : Lighting.GetColor(segment.Position.ToTileCoordinates(), Color.Value);

                Main.spriteBatch.Draw(Texture, segment.Position - Main.screenPosition, rectangle, color, segment.Rotation, origin, 1f, SpriteEffects.None, 0f);
            }
        }

        public class Gradient : IDrawYoyoStringSegment
        {
            public Texture2D Texture { get; init; }
            public ColorData[] Colors { get; init; }

            public Gradient(params ColorData[] colors) : this(null, colors) {}

            public Gradient(Texture2D texture, params ColorData[] colors)
            {
                Texture = texture ?? TextureAssets.FishingLine.Value;
                Colors = colors;
            }

            public void Draw(int segmentCount, YoyoStringSegment segment)
            {
                var rectangle = new Rectangle(0, 0, Texture.Width, (int)segment.Width);
                var origin = new Vector2(Texture.Width * 0.5f, 0f);      
                var color = ColorUtils.MultipleLerp(segment.Index / (float)segmentCount, Colors.Select(x => x.Glow ? x.Value : Lighting.GetColor(segment.Position.ToTileCoordinates(), x.Value)).ToArray());

                Main.spriteBatch.Draw(Texture, segment.Position - Main.screenPosition, rectangle, color, segment.Rotation, origin, 1f, SpriteEffects.None, 0f);
            }
        }
    }

    public sealed class YoyoStringRenderer
    {
        private readonly IDrawYoyoStringSegment _segmentRenderer;
        private readonly List<YoyoStringSegment> _segments;
        private Rectangle _projHitbox;
        private Vector2 _projVelocity;
        private Vector2 _startPosition;

        public Projectile Projectile { get; init; }

        public YoyoStringRenderer(Projectile proj, IDrawYoyoStringSegment segmentRenderer)
        {
            _segmentRenderer = segmentRenderer;
            _segments = new();

            // Да, проверки идут после присваивания. Так и задумано.
            Projectile = proj;

            if (proj is null)
                throw new ArgumentNullException(nameof(proj));

            if (!proj.IsYoyo())
                throw new ArgumentException("Projectile is not a yoyo", nameof(proj));
        }

        public void Draw(Vector2 startPosition)
        {
            if (Projectile is null)
                return;

            if (_startPosition != startPosition || _projVelocity != Projectile.velocity || _projHitbox != Projectile.Hitbox)
            {
                _startPosition = startPosition;
                _projVelocity = Projectile.velocity;
                _projHitbox = Projectile.Hitbox;

                PrepareSegments();
            }

            foreach (var segment in _segments)
                _segmentRenderer.Draw(_segments.Count, segment);
        }

        private void PrepareSegments()
        {
            const float vanillaLineHeightValue = 12f;

            _segments.Clear();

            var endPosition = Projectile.Center;   
            var x = endPosition.X - _startPosition.X;
            var y = endPosition.Y - _startPosition.Y;
            var shouldAddNextSegment = true;
            var isFirstSegment = true;

            if ((double)x == 0.0 && (double)y == 0.0)
            {
                shouldAddNextSegment = false;
            }
            else
            {
                var num4 = _segmentRenderer.Texture.Height / (float)Math.Sqrt((double)x * (double)x + (double)y * (double)y);
                var num5 = x * num4;
                var num6 = y * num4;

                _startPosition.X -= num5 * 0.1f;
                _startPosition.Y -= num6 * 0.1f;
                x = endPosition.X - _startPosition.X;
                y = endPosition.Y - _startPosition.Y;
            }

            var segmentStartPos = _startPosition;

            while (shouldAddNextSegment)
            {
                var width = (float)_segmentRenderer.Texture.Height;
                var f1 = (float)Math.Sqrt((double)x * (double)x + (double)y * (double)y);
                var f2 = f1;

                if (float.IsNaN(f1) || float.IsNaN(f2))
                {
                    shouldAddNextSegment = false;
                }
                else
                {
                    var factor = _segmentRenderer.Texture.Height / vanillaLineHeightValue;

                    if ((double)f1 < 20.0 * factor)
                    {
                        width = f1 - 8f * factor;
                        shouldAddNextSegment = false;
                    }

                    var num8 = _segmentRenderer.Texture.Height / f1;
                    var num9 = x * num8;
                    var num10 = y * num8;

                    if (isFirstSegment)
                    {
                        isFirstSegment = false;
                    }
                    else
                    {
                        segmentStartPos.X += num9;
                        segmentStartPos.Y += num10;
                    }

                    x = Projectile.position.X + Projectile.width * 0.5f - segmentStartPos.X;
                    y = Projectile.position.Y + Projectile.height * 0.1f - segmentStartPos.Y;

                    if ((double)f2 > _segmentRenderer.Texture.Height)
                    {
                        var num12 = Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y);

                        if ((double)num12 > 16.0)
                            num12 = 16f;

                        var num13 = (float)(1.0 - (double)num12 / 16.0);
                        var num14 = 0.3f * num13;
                        var num15 = f2 / 80f;

                        if ((double)num15 > 1.0)
                            num15 = 1f;

                        var num16 = num14 * num15;

                        if ((double)num16 < 0.0)
                            num16 = 0.0f;

                        var num17 = num16 * num15 * 0.5f;

                        if ((double)y > 0.0)
                        {
                            y *= 1f + num17;
                            x *= 1f - num17;
                        }
                        else
                        {
                            var num18 = Math.Abs(Projectile.velocity.X) / 3f;

                            if ((double)num18 > 1.0)
                                num18 = 1f;

                            var num19 = num18 - 0.5f;
                            var num20 = num17 * num19;

                            if ((double)num20 > 0.0f)
                                num20 *= 2f;

                            y *= 1f + num20;
                            x *= 1f - num20;
                        }
                    }
                    
                    var position = new Vector2(segmentStartPos.X, (float)(segmentStartPos.Y + _segmentRenderer.Texture.Height * 0.5f + (vanillaLineHeightValue - _segmentRenderer.Texture.Height) * 0.5f));
                    var rotation = (float)Math.Atan2((double)y, (double)x) - MathHelper.PiOver2;

                    _segments.Add(new YoyoStringSegment(Projectile, _segments.Count, position, rotation, width));
                }
            }
        }
    }
}