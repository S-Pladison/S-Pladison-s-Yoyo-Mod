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
            set => SetPosition(value);
        }

        public float Thickness
        {
            get => innerThickness;
            set => SetThickness(value);
        }

        public float Radius
        {
            get => innerRadius;
            set => SetRadius(value);
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
            if (innerPosition == position)
                return this;

            lineRenderer.Offset(position - innerPosition);
            innerPosition = position;
            return this;
        }

        public RingRenderer SetThickness(float thickness)
        {
            if (innerRadius == thickness)
                return this;

            innerThickness = thickness;
            lineRenderer.SetWidth(thickness);
            return this;
        }

        public RingRenderer SetRadius(float radius)
        {
            if (innerRadius == radius)
                return this;

            innerRadius = radius;
            RecalculateMesh();
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