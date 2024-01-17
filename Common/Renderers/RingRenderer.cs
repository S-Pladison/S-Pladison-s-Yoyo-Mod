using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;

namespace SPYoyoMod.Common.Renderers
{
    public class RingRenderer
    {
        public int PointCount { get; init; }

        public Vector2 Position
        {
            get => innerPosition;
            set
            {
                if (innerPosition == value) return;

                lineRenderer.Offset(value - innerPosition);
                innerPosition = value;
            }
        }

        public float Thickness
        {
            get => innerThickness;
            set
            {
                if (innerThickness == value) return;

                innerThickness = value;
                lineRenderer.Width = value;
            }
        }

        public float Radius
        {
            get => innerRadius;
            set
            {
                if (innerRadius == value) return;

                innerRadius = value;
                RecalculateMesh();
            }
        }

        private readonly LineRenderer lineRenderer;

        private Vector2 innerPosition;
        private float innerThickness;
        private float innerRadius;

        public RingRenderer(int pointCount, float thickness, float radius)
        {
            PointCount = pointCount;

            lineRenderer = new LineRenderer(pointCount, thickness, true);

            innerPosition = Vector2.Zero;
            innerThickness = thickness;
            innerRadius = radius;

            RecalculateMesh();
        }

        public RingRenderer SetPosition(Vector2 position)
        {
            Position = position;
            return this;
        }

        public void Draw(Asset<Effect> effect)
        {
            Draw(effect.Value);
        }

        public void Draw(Effect effect)
        {
            lineRenderer.Draw(effect);
        }

        private void RecalculateMesh()
        {
            var points = new List<Vector2>();

            var step = MathHelper.TwoPi / PointCount;

            for (int i = 0; i < PointCount; i++)
            {
                var angle = step * i;

                points.Add(Position + Vector2.UnitX.RotatedBy(angle) * Radius);
            }

            lineRenderer.SetPoints(points);
        }
    }
}