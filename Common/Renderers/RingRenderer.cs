using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Runtime.CompilerServices;
using Terraria;

namespace SPYoyoMod.Common.Renderers
{
    public class RingRenderer : IDisposable
    {
        public int PointCount
        {
            get => innerPointCount;
            set => SetPointCount(value);
        }

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

        private PrimitiveRenderer renderer;
        private VertexPositionColorTexture[] vertices;
        private short[] indices;

        private bool isDirty;
        private int maxPointCount;

        private int innerPointCount;
        private Vector2 innerPosition;
        private float innerRadius;
        private float innerThickness;
        private float halfThickness;

        public RingRenderer(int pointCount, float thickness, float radius)
        {
            vertices = Array.Empty<VertexPositionColorTexture>();
            indices = Array.Empty<short>();

            SetPointCount(pointCount);
            SetPosition(Vector2.Zero);
            SetThickness(thickness);
            SetRadius(radius);

            Recalculate();
        }

        public RingRenderer SetPointCount(int pointCount)
        {
            if (innerPointCount == pointCount)
                return this;

            innerPointCount = pointCount;
            isDirty = true;

            return this;
        }

        public RingRenderer SetPosition(Vector2 position)
        {
            if (innerPosition == position)
                return this;

            Offset(position - innerPosition);
            innerPosition = position;

            return this;
        }

        public RingRenderer SetThickness(float thickness)
        {
            if (innerRadius == thickness)
                return this;

            innerThickness = thickness;
            halfThickness = thickness / 2f;
            isDirty = true;

            return this;
        }

        public RingRenderer SetRadius(float radius)
        {
            if (innerRadius == radius)
                return this;

            innerRadius = radius;
            isDirty = true;

            return this;
        }

        public void Draw(Asset<Effect> effect)
        {
            Draw(effect.Value);
        }

        public void Draw(Effect effect)
        {
            if (isDirty)
            {
                Recalculate();

                isDirty = false;
            }

            var vertexCount = 2 * (PointCount + 1);
            var indexCount = 6 * PointCount;

            renderer.Draw(effect, vertexCount, indexCount / 3);
        }

        public void Dispose()
        {
            renderer?.Dispose();
        }

        private void Offset(Vector2 offset)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Position.X += offset.X;
                vertices[i].Position.Y += offset.Y;
            }

            renderer.SetVertices(vertices);
        }

        private void Recalculate()
        {
            if (maxPointCount < PointCount)
            {
                var oldMaxPointCount = maxPointCount;

                maxPointCount = PointCount;

                var maxVertices = 2 * (maxPointCount + 1);
                var maxIndices = 6 * maxPointCount;

                renderer = new PrimitiveRenderer(maxVertices, maxIndices);

                Array.Resize(ref vertices, maxVertices);
                Array.Resize(ref indices, maxIndices);

                CalculateVertexIndices(oldMaxPointCount, maxPointCount);
                CalculateVertexColors(oldMaxPointCount, maxPointCount);
            }

            CalculateVertexPositionsAndUVs();

            renderer.SetIndices(indices);
            renderer.SetVertices(vertices);
        }

        private void CalculateVertexPositionsAndUVs()
        {
            var vertexIndex = 0;
            var step = MathHelper.TwoPi / PointCount;
            var points = new Vector2[PointCount + 1];

            for (int i = 0; i <= PointCount; i++)
            {
                var angle = step * i;
                var direction = Vector2.UnitX.RotatedBy(angle);
                var pointPosition = Position + direction * Radius;
                var offset = direction * halfThickness;

                points[i] = pointPosition;

                AddVertexPosition(ref vertexIndex, pointPosition + offset);
                AddVertexPosition(ref vertexIndex, pointPosition - offset);
            }

            CalculateFactorsFromStartToEnd(points, out float[] factorsFromStartToEnd);

            vertexIndex = 0;

            for (int i = 0; i < factorsFromStartToEnd.Length; i++)
            {
                AddVertexUV(ref vertexIndex, new Vector2(factorsFromStartToEnd[i], 0));
                AddVertexUV(ref vertexIndex, new Vector2(factorsFromStartToEnd[i], 1));
            }
        }

        private void CalculateFactorsFromStartToEnd(Vector2[] points, out float[] factorsFromStartToEnd)
        {
            var accumulativeLength = 0f;
            var lengths = new float[PointCount];
            var totalLength = 0f;

            factorsFromStartToEnd = new float[PointCount + 1];

            for (int i = 0; i < PointCount; i++)
            {
                int j = (i + 1) % PointCount;

                lengths[i] = Vector2.Distance(points[i], points[j]);
                totalLength += lengths[i];
            }

            for (int i = 0; i < PointCount; i++)
            {
                accumulativeLength += lengths[i];
                factorsFromStartToEnd[i + 1] = accumulativeLength / totalLength;
            }
        }

        private void AddVertexPosition(ref int vertexIndex, Vector2 position)
        {
            vertices[vertexIndex++].Position = new Vector3(position, 0);
        }

        private void AddVertexUV(ref int vertexIndex, Vector2 uv)
        {
            vertices[vertexIndex++].TextureCoordinate = uv;
        }

        private void CalculateVertexIndices(int start, int end)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Add(ref int index, int value)
            {
                indices[index++] = (short)value;
            }

            for (int i = start; i < end; i++)
            {
                int index = i * 6;
                int i2 = i * 2;
                int j2 = (i + 1) * 2;

                Add(ref index, i2);
                Add(ref index, i2 + 1);
                Add(ref index, j2 + 1);
                Add(ref index, j2 + 1);
                Add(ref index, j2);
                Add(ref index, i2);
            }
        }

        private void CalculateVertexColors(int start, int end)
        {
            for (int i = start; i <= end; i++)
            {
                int index = i * 2;

                vertices[index].Color = Color.White;
                vertices[index + 1].Color = Color.White;
            }
        }
    }
}