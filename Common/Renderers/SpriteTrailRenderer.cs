using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Utils;
using System;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace SPYoyoMod.Common.Renderers
{
    public class SpriteTrailRenderer
    {
        private struct Point
        {
            public Vector2 Position;
            public float Rotation;
        }

        public Asset<Texture2D> Texture { get; set; }
        public Rectangle? Frame { get; set; }
        public Vector2 Origin { get; set; }
        public SpriteEffects SpriteEffects { get; set; }

        public int MaxPoints
        {
            get => innerMaxPoints;
            set => SetMaxPoints(value);
        }

        public ColorDelegate Color
        {
            get => innerColor;
            set => SetColor(value);
        }

        public ScaleDelegate Scale
        {
            get => innerScale;
            set => SetScale(value);
        }

        private Point[] points;
        private int innerMaxPoints;
        private int activePoints;
        private ColorDelegate innerColor;
        private ScaleDelegate innerScale;

        public SpriteTrailRenderer(int maxPoints, Asset<Texture2D> texture, Vector2 origin, SpriteEffects spriteEffects)
        {
            points = Array.Empty<Point>();

            SetMaxPoints(maxPoints);
            SetTexture(texture);
            SetOrigin(origin);
            SetSpriteEffects(spriteEffects);
            SetFadingColor(XnaColor.White);
            SetScale(_ => 1f);
        }

        public SpriteTrailRenderer SetTexture(Asset<Texture2D> texture)
        {
            Texture = texture;
            return this;
        }

        public SpriteTrailRenderer SetFrame(Rectangle? frame)
        {
            Frame = frame;
            return this;
        }

        public SpriteTrailRenderer SetOrigin(Vector2 origin)
        {
            Origin = origin;
            return this;
        }

        public SpriteTrailRenderer SetSpriteEffects(SpriteEffects spriteEffects)
        {
            SpriteEffects = spriteEffects;
            return this;
        }

        public SpriteTrailRenderer SetFadingColor(XnaColor color)
            => SetColor(f => XnaColor.Lerp(color, XnaColor.Transparent, f));

        public SpriteTrailRenderer SetColor(ColorDelegate color)
        {
            innerColor = color;
            return this;
        }

        public SpriteTrailRenderer SetFadingScale(float scale)
            => SetScale(f => MathHelper.Lerp(scale, 0f, f));

        public SpriteTrailRenderer SetScale(ScaleDelegate scale)
        {
            innerScale = scale;
            return this;
        }

        public SpriteTrailRenderer SetMaxPoints(int maxPoints)
        {
            if (innerMaxPoints.Equals(maxPoints))
                return this;

            var oldMaxPoints = innerMaxPoints;
            innerMaxPoints = maxPoints;

            if (oldMaxPoints < maxPoints)
                Array.Resize(ref points, maxPoints);

            return this;
        }

        public SpriteTrailRenderer SetNextPoint(Vector2 pointPosition, float headRotation)
        {
            for (int i = MaxPoints - 1; i > 0; --i)
                points[i] = points[i - 1];

            points[0].Position = pointPosition;
            points[0].Rotation = headRotation;

            activePoints = Math.Min(MaxPoints, activePoints + 1);

            return this;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 positionOffset, XnaColor colorMultiplier)
        {
            for (int i = 0; i < activePoints; i++)
            {
                var factor = (float)i / activePoints;
                var color = ColorUtils.Multiply(innerColor(factor), colorMultiplier);
                var scale = innerScale(factor);

                spriteBatch.Draw(Texture.Value, points[i].Position + positionOffset, Frame, color, points[i].Rotation, Origin, scale, SpriteEffects, 0f);
            }
        }

        public delegate XnaColor ColorDelegate(float factorFromStartToEnd);
        public delegate float ScaleDelegate(float factorFromStartToEnd);
    }
}