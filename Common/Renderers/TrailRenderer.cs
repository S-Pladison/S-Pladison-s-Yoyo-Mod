using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Runtime.CompilerServices;
using Terraria;

namespace SPYoyoMod.Common.Renderers
{
    public class TrailRenderer : IDisposable
    {
        public int MaxPoints
        {
            get => innerMaxPoints;
            set => SetMaxPoints(value);
        }

        public WidthDelegate Width
        {
            get => innerWidth;
            set => SetWidth(value);
        }

        private PrimitiveRenderer renderer;
        private VertexPositionColorTexture[] vertices;
        private short[] indices;
        private Vector2[] points;

        private bool isDirty;
        private int maxSegmentCount;

        private int innerMaxPoints;
        private int activePoints;
        private WidthDelegate innerWidth;

        public TrailRenderer(int maxPoints)
        {
            vertices = Array.Empty<VertexPositionColorTexture>();
            indices = Array.Empty<short>();
            points = Array.Empty<Vector2>();

            SetMaxPoints(maxPoints);
            SetWidth(_ => 16f);
        }

        public TrailRenderer SetMaxPoints(int maxPoints)
        {
            if (innerMaxPoints.Equals(maxPoints))
                return this;

            var oldMaxPoints = innerMaxPoints;
            innerMaxPoints = maxPoints;

            if (oldMaxPoints < maxPoints)
                Array.Resize(ref points, maxPoints);

            isDirty = true;

            return this;
        }

        public TrailRenderer SetWidth(WidthDelegate width)
        {
            innerWidth = width;
            isDirty = true;

            return this;
        }

        public TrailRenderer SetNextPoint(Vector2 pointPosition)
        {
            for (var i = MaxPoints - 1; i > 0; --i)
                points[i] = points[i - 1];

            points[0] = pointPosition;
            activePoints = Math.Min(MaxPoints, activePoints + 1);
            isDirty = true;

            return this;
        }

        public void Draw(Asset<Effect> effect)
        {
            Draw(effect.Value);
        }

        public void Draw(Effect effect)
        {
            if (activePoints < 2) return;

            if (isDirty)
            {
                Recalculate();

                isDirty = false;
            }

            var count = activePoints - 1;
            var vertexCount = 2 * (count + 1);
            var indexCount = 6 * count;

            renderer.Draw(effect, vertexCount, indexCount / 3);
        }

        public float GetTotalLengthBetweenPoints()
        {
            if (activePoints <= 1) return 0;

            var result = 0f;

            for (var i = 1; i < activePoints; i++)
            {
                result += Vector2.Distance(points[i], points[i - 1]);
            }

            return result;
        }

        public void Dispose()
        {
            renderer?.Dispose();
        }

        private void Recalculate()
        {
            var segmentCount = points.Length - 1;

            if (maxSegmentCount < segmentCount)
            {
                var oldMaxSegmentCount = maxSegmentCount;

                maxSegmentCount = segmentCount;

                var maxVertices = 2 * (maxSegmentCount + 1);
                var maxIndices = 6 * maxSegmentCount;

                renderer = new PrimitiveRenderer(maxVertices, maxIndices);

                Array.Resize(ref vertices, maxVertices);
                Array.Resize(ref indices, maxIndices);

                CalculateVertexIndices(oldMaxSegmentCount, maxSegmentCount);
                CalculateVertexColors(oldMaxSegmentCount, maxSegmentCount);

                renderer.SetIndices(indices);
            }

            CalculateFactorsFromStartToEnd(out var factorsFromStartToEnd);
            CalculateVertexPositions(factorsFromStartToEnd);
            CalculateVertexUVs(factorsFromStartToEnd);

            renderer.SetVertices(vertices);
        }

        private void CalculateFactorsFromStartToEnd(out float[] factorsFromStartToEnd)
        {
            var segmentCount = activePoints - 1;
            var accumulativeLength = 0f;
            var lengths = new float[segmentCount];
            var totalLength = 0f;

            factorsFromStartToEnd = new float[segmentCount];

            for (var i = 0; i < activePoints - 1; i++)
            {
                lengths[i] = Vector2.Distance(points[i], points[i + 1]);
                totalLength += lengths[i];
            }

            for (var i = 0; i < segmentCount; i++)
            {
                accumulativeLength += lengths[i];
                factorsFromStartToEnd[i] = accumulativeLength / totalLength;
            }
        }

        private void CalculateVertexPositions(float[] factorsFromStartToEnd)
        {
            var vertexIndex = 0;
            var segmentCount = activePoints - 1;

            var normal = (points[1] - points[0]).SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2);
            var halfWidth = innerWidth(0f) / 2f;
            var offset = normal * halfWidth;

            AddVertexPosition(ref vertexIndex, points[0] + offset);
            AddVertexPosition(ref vertexIndex, points[0] - offset);

            for (var i = 1; i < activePoints; i++)
            {
                normal = (points[i] - points[i - 1]).SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2);
                halfWidth = innerWidth(factorsFromStartToEnd[i - 1]) / 2f;
                offset = normal * halfWidth;

                AddVertexPosition(ref vertexIndex, points[i] + offset);
                AddVertexPosition(ref vertexIndex, points[i] - offset);
            }
        }

        private void AddVertexPosition(ref int vertexIndex, Vector2 position)
        {
            vertices[vertexIndex++].Position = new Vector3(position, 0);
        }

        private void CalculateVertexUVs(float[] factorsFromStartToEnd)
        {
            var vertexIndex = 0;

            AddVertexUV(ref vertexIndex, Vector2.Zero);
            AddVertexUV(ref vertexIndex, Vector2.UnitY);

            for (var i = 0; i < factorsFromStartToEnd.Length; i++)
            {
                AddVertexUV(ref vertexIndex, new Vector2(factorsFromStartToEnd[i], 0));
                AddVertexUV(ref vertexIndex, new Vector2(factorsFromStartToEnd[i], 1));
            }
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

            for (var i = start; i < end; i++)
            {
                var index = i * 6;
                var i2 = i * 2;
                var j2 = (i + 1) * 2;

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
            for (var i = start; i <= end; i++)
            {
                var index = i * 2;

                vertices[index].Color = Color.White;
                vertices[index + 1].Color = Color.White;
            }
        }

        public delegate float WidthDelegate(float factorFromStartToEnd);
    }
}