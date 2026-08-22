using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;

namespace SPYoyoMod.Core.Graphics.Renderers
{
    /// <summary>
    /// Контекст отрисовки нити снарядов йо-йо.
    /// </summary>
    public readonly struct YoyoStringRendererContext(Projectile proj, Vector2 start, Vector2 offset = default)
    {
        /// <summary>
        /// Снаряд, до которого будет отрисовываться нить (начиная со старта до самого снаряда). Если значение равно null, то нить рисоваться не будет.
        /// </summary>
        public readonly Projectile Projectile = proj;

        /// <summary>
        /// Позиция старта (начало отрисовки нити).
        /// </summary>
        public readonly Vector2 Start = start;

        /// <summary>
        /// Смещение всех позиций при отрисовке сегментов нити. Выбирай -<see cref="Main.screenPosition"/>, не ошибешься.
        /// </summary>
        public readonly Vector2 Offset = offset;

        /// <summary>
        /// Собирает контекст из снаряда и точки крепления нити.
        /// </summary>
        public static YoyoStringRendererContext FromProjectile(Projectile proj, Vector2 mountedCenter)
            => new(
                proj,
                mountedCenter + proj.GetOwner()?.gfxOffY * Vector2.UnitY ?? Vector2.Zero,
                -Main.screenPosition
            );
    }

    /// <summary>
    /// Структура, хранящая в себе информацию о сегменте нити йо-йо.
    /// </summary>
    public readonly struct YoyoStringSegment(int index, Vector2 position, float rotation, float length)
    {
        /// <summary>
        /// Уникальный индекс сегмента при его отрисовке во время отрисовки нити йо-йо.
        /// </summary>
        public readonly int Index = index;

        /// <summary>
        /// Позиция сегмента в мире.
        /// </summary>
        public readonly Vector2 Position = position;

        /// <summary>
        /// Значение вращения сегмента.
        /// </summary>
        public readonly float Rotation = rotation;

        /// <summary>
        /// Длина сегмента. Если сегмент не влезает, длина будет уменьшена.
        /// </summary>
        public readonly float Length = length;

        public void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 origin, Vector2 offset, Color color)
        {
            spriteBatch.Draw(texture, Position + offset, new Rectangle(0, 0, texture.Width, (int)Length), color, Rotation, origin, 1f, SpriteEffects.None, 0f);
        }
    }

    /// <summary>
    /// Интерфейс, описывающий логику отрисовки нити йо-йо.
    /// </summary>
    public interface IDrawYoyoStringSegments
    {
        /// <summary>
        /// Текстура сегмента нити. Высота задаёт шаг раскладки сегментов.
        /// </summary>
        Texture2D Texture { get; }

        void Draw(SpriteBatch spriteBatch, in YoyoStringRendererContext context, IReadOnlyList<YoyoStringSegment> segments);

        public record struct ColorData(Color Value, bool Glow)
        {
            public static implicit operator ColorData((Color Value, bool Glow) tuple)
                => new(tuple.Value, tuple.Glow);
        }

        /// <summary>
        /// Класс, отрисовывающий ванильную нить йо-йо (с учетом аксессуаров и освещения).
        /// </summary>
        public sealed class Vanilla : IDrawYoyoStringSegments
        {
            public Texture2D Texture => TextureAssets.FishingLine.Value;

            public void Draw(SpriteBatch spriteBatch, in YoyoStringRendererContext context, IReadOnlyList<YoyoStringSegment> segments)
            {
                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "TryApplyingPlayerStringColor")]
                extern static Color TryApplyingPlayerStringColor(Main _, int playerStringColor, Color defaultColor);

                var stringColor = TryApplyingPlayerStringColor(null, context.Projectile.GetOwner().stringColor, Color.White with { A = (byte)(255 * 0.4f) });
                var origin = new Vector2(Texture.Width * 0.5f, 0f);

                foreach (var segment in segments)
                {
                    var color = Lighting.GetColor(segment.Position.ToTileCoordinates(), stringColor);
                    color = new Color((byte)(color.R * 0.5f), (byte)(color.G * 0.5f), (byte)(color.B * 0.5f), (byte)(color.A * 0.5f));

                    segment.Draw(spriteBatch, Texture, origin, context.Offset, color);
                }
            }
        }

        /// <summary>
        /// Класс, отрисовывающий примитивную нить йо-йо.
        /// </summary>
        public sealed class Default(Texture2D texture, ColorData color) : IDrawYoyoStringSegments
        {
            public Texture2D Texture { get; } = texture ?? TextureAssets.FishingLine.Value;
            public ColorData Color { get; } = color;

            public Default(ColorData color) : this(null, color) { }

            public void Draw(SpriteBatch spriteBatch, in YoyoStringRendererContext context, IReadOnlyList<YoyoStringSegment> segments)
            {
                var origin = new Vector2(Texture.Width * 0.5f, 0f);

                foreach (var segment in segments)
                {
                    var color = Color.Glow ? Color.Value : Lighting.GetColor(segment.Position.ToTileCoordinates(), Color.Value);
                    segment.Draw(spriteBatch, Texture, origin, context.Offset, color);
                }
            }
        }

        /// <summary>
        /// Класс, отрисовывающий градиентную нить йо-йо (с учетом нескольких цветов).
        /// </summary>
        public sealed class Gradient(Texture2D texture, params ColorData[] colors) : IDrawYoyoStringSegments
        {
            private readonly Color[] _lerpColors = new Color[colors.Length];

            public Texture2D Texture { get; } = texture ?? TextureAssets.FishingLine.Value;
            public ColorData[] Colors { get; } = colors;

            public Gradient(params ColorData[] colors) : this(null, colors) { }

            public void Draw(SpriteBatch spriteBatch, in YoyoStringRendererContext context, IReadOnlyList<YoyoStringSegment> segments)
            {
                var origin = new Vector2(Texture.Width * 0.5f, 0f);
                var segmentCount = segments.Count;

                foreach (var segment in segments)
                {
                    var tileCoords = segment.Position.ToTileCoordinates();

                    for (var i = 0; i < Colors.Length; i++)
                    {
                        var colorData = Colors[i];
                        _lerpColors[i] = colorData.Glow ? colorData.Value : Lighting.GetColor(tileCoords, colorData.Value);
                    }

                    var color = ColorUtils.MultipleLerp(segment.Index / (float)segmentCount, _lerpColors);
                    segment.Draw(spriteBatch, Texture, origin, context.Offset, color);
                }
            }
        }
    }

    /// <summary>
    /// Класс-отрисовщик нити от йо-йо.
    /// </summary>
    public sealed class YoyoStringRenderer(IDrawYoyoStringSegments segmentRenderer)
    {
        private readonly IDrawYoyoStringSegments _segmentRenderer = segmentRenderer;
        private readonly List<YoyoStringSegment> _segments = [];

        private Rectangle _projHitbox;
        private Vector2 _projVelocity;
        private Vector2 _startPosition;

        public void Render(SpriteBatch spriteBatch, in YoyoStringRendererContext context)
        {
            if (context.Projectile is null)
                return;

            var proj = context.Projectile;

            if (_projVelocity != proj.velocity || _projHitbox != proj.Hitbox || _startPosition != context.Start)
            {
                _projVelocity = proj.velocity;
                _projHitbox = proj.Hitbox;
                _startPosition = context.Start;

                CalculateSegments(proj, context.Start);
            }

            _segmentRenderer.Draw(spriteBatch, context, _segments);
        }

        private void CalculateSegments(Projectile proj, Vector2 startPosition)
        {
            const float vanillaLineHeight = 12f;

            _segments.Clear();

            var textureHeight = _segmentRenderer.Texture.Height;
            var endPosition = proj.Center;
            var x = endPosition.X - startPosition.X;
            var y = endPosition.Y - startPosition.Y;
            var shouldAddNextSegment = true;
            var isFirstSegment = true;

            if ((double)x == 0.0 && (double)y == 0.0)
            {
                shouldAddNextSegment = false;
            }
            else
            {
                var inverseDistance = textureHeight / (float)Math.Sqrt((double)x * (double)x + (double)y * (double)y);
                var stepX = x * inverseDistance;
                var stepY = y * inverseDistance;

                startPosition.X -= stepX * 0.1f;
                startPosition.Y -= stepY * 0.1f;
                x = endPosition.X - startPosition.X;
                y = endPosition.Y - startPosition.Y;
            }

            var segmentStartPos = startPosition;

            while (shouldAddNextSegment)
            {
                var length = (float)textureHeight;
                var remainingDistance = (float)Math.Sqrt((double)x * (double)x + (double)y * (double)y);

                if (float.IsNaN(remainingDistance))
                {
                    shouldAddNextSegment = false;
                    continue;
                }

                var heightScale = textureHeight / vanillaLineHeight;

                if ((double)remainingDistance < 20.0 * heightScale)
                {
                    length = remainingDistance - 8f * heightScale;
                    shouldAddNextSegment = false;
                }

                var inverseRemaining = textureHeight / remainingDistance;
                var advanceX = x * inverseRemaining;
                var advanceY = y * inverseRemaining;

                if (isFirstSegment)
                {
                    isFirstSegment = false;
                }
                else
                {
                    segmentStartPos.X += advanceX;
                    segmentStartPos.Y += advanceY;
                }

                x = proj.position.X + proj.width * 0.5f - segmentStartPos.X;
                y = proj.position.Y + proj.height * 0.1f - segmentStartPos.Y;

                if ((double)remainingDistance > textureHeight)
                {
                    var speed = Math.Abs(proj.velocity.X) + Math.Abs(proj.velocity.Y);

                    if ((double)speed > 16.0)
                        speed = 16f;

                    var speedFactor = (float)(1.0 - (double)speed / 16.0);
                    var sag = 0.3f * speedFactor;
                    var lengthFactor = remainingDistance / 80f;

                    if ((double)lengthFactor > 1.0)
                        lengthFactor = 1f;

                    sag *= lengthFactor;

                    if ((double)sag < 0.0)
                        sag = 0.0f;

                    var sagAmount = sag * lengthFactor * 0.5f;

                    if ((double)y > 0.0)
                    {
                        y *= 1f + sagAmount;
                        x *= 1f - sagAmount;
                    }
                    else
                    {
                        var horizontalSpeedFactor = Math.Abs(proj.velocity.X) / 3f;

                        if ((double)horizontalSpeedFactor > 1.0)
                            horizontalSpeedFactor = 1f;

                        var downwardSag = sagAmount * (horizontalSpeedFactor - 0.5f);

                        if ((double)downwardSag > 0.0f)
                            downwardSag *= 2f;

                        y *= 1f + downwardSag;
                        x *= 1f - downwardSag;
                    }
                }

                var position = new Vector2(segmentStartPos.X, (float)(segmentStartPos.Y + textureHeight * 0.5f + (vanillaLineHeight - textureHeight) * 0.5f));
                var rotation = (float)Math.Atan2((double)y, (double)x) - MathHelper.PiOver2;

                _segments.Add(new YoyoStringSegment(_segments.Count, position, rotation, length));
            }
        }
    }
}
