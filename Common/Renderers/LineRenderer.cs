using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;

namespace SPYoyoMod.Common.Renderers
{
    public class LineRenderer : IDisposable
    {
        private class Line
        {
            private static readonly float parallelThreshold = 0.01f;

            public Vector2 Offset;
            public Vector2 Direction;

            public Vector2 Evaluate(float t)
            {
                return Offset + Direction * t;
            }

            public bool IsParallel(Line other)
            {
                return Vector3.Cross(new Vector3(Vector2.Normalize(Direction), 0), new Vector3(Vector2.Normalize(other.Direction), 0)).Length() < parallelThreshold;
            }

            public Vector2 Intersection(Line other)
            {
                var solutions = SolveLinearEquation(
                    a0: Vector2.Dot(Direction, Direction),
                    b0: -Vector2.Dot(other.Direction, Direction),
                    c0: Vector2.Dot(Offset - other.Offset, Direction),
                    a1: Vector2.Dot(Direction, other.Direction),
                    b1: -Vector2.Dot(other.Direction, other.Direction),
                    c1: Vector2.Dot(Offset - other.Offset, other.Direction)
                );

                return Evaluate(solutions.Item1);
            }

            private static Tuple<float, float> SolveLinearEquation(float a0, float b0, float c0, float a1, float b1, float c1)
            {
                var v = (a1 * c0 - a0 * c1) / (a0 * b1 - a1 * b0);
                var t = (-c0 - b0 * v) / a0;

                return new Tuple<float, float>(t, v);
            }
        }

        public LineRendererType Type
        {
            get => innerType;
            set => SetType(value);
        }

        public bool Loop
        {
            get => innerLoop;
            set => SetLoop(value);
        }

        public float Width
        {
            get => innerWidth;
            set => SetWidth(value);
        }

        private PrimitiveRenderer renderer;
        private VertexPositionColorTexture[] vertices;
        private short[] indices;
        private IList<Vector2> points;

        private bool isDirty;
        private int maxSegmentCount;

        private LineRendererType innerType;
        private bool innerLoop;
        private float innerWidth;
        private float halfWidth;

        public LineRenderer(float width, bool loop)
        {
            vertices = Array.Empty<VertexPositionColorTexture>();
            indices = Array.Empty<short>();
            points = new List<Vector2>();

            SetWidth(width);
            SetLoop(loop);
        }

        public LineRenderer SetPoints(IList<Vector2> points)
        {
            this.points = points;
            isDirty = true;

            return this;
        }

        public LineRenderer SetType(LineRendererType type)
        {
            if (innerType == type)
                return this;

            innerType = type;
            isDirty = true;

            return this;
        }

        public LineRenderer SetWidth(float width)
        {
            if (innerWidth == width)
                return this;

            innerWidth = width;
            halfWidth = innerWidth / 2f;
            isDirty = true;

            return this;
        }

        public LineRenderer SetLoop(bool value)
        {
            if (innerLoop == value)
                return this;

            innerLoop = value;
            isDirty = true;

            return this;
        }

        public void Draw(Asset<Effect> effect)
        {
            Draw(effect.Value);
        }

        public void Draw(Effect effect)
        {
            if (points.Count < 2) return;

            if (isDirty)
            {
                Recalculate();

                isDirty = false;
            }

            var count = points.Count - (Loop ? 0 : 1);
            var vertexCount = 2 * (count + 1);
            var indexCount = 6 * count;

            renderer.Draw(effect, vertexCount, indexCount / 3);
        }

        public void Dispose()
        {
            renderer?.Dispose();
        }

        private void Recalculate()
        {
            var segmentCount = points.Count + (Loop ? 0 : -1);

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
            CalculateVertexPositions();
            CalculateVertexUVs(factorsFromStartToEnd);

            renderer.SetVertices(vertices);
        }

        private void CalculateFactorsFromStartToEnd(out float[] factorsFromStartToEnd)
        {
            var segmentCount = Loop ? points.Count : points.Count - 1;
            var accumulativeLength = 0f;
            var lengths = new float[segmentCount];
            var totalLength = 0f;

            factorsFromStartToEnd = new float[segmentCount];

            for (var i = 0; i < points.Count - 1; i++)
            {
                lengths[i] = Vector2.Distance(points[i], points[i + 1]);
                totalLength += lengths[i];
            }

            if (Loop)
            {
                lengths[^1] = Vector2.Distance(points[^1], points[0]);
                totalLength += lengths[^1];
            }

            for (var i = 0; i < segmentCount; i++)
            {
                accumulativeLength += lengths[i];
                factorsFromStartToEnd[i] = accumulativeLength / totalLength;
            }
        }

        private void CalculateVertexPositions()
        {
            switch (Type)
            {
                case LineRendererType.Primitive:
                    CalculatePrimitiveVertexPositions();
                    return;
                case LineRendererType.Advanced:
                    CalculateAdvancedVertexPositions();
                    return;
                default:
                    throw new NotImplementedException();
            }
        }

        private void CalculatePrimitiveVertexPositions()
        {
            var vertexIndex = 0;

            var normal = RotateClockwiseNinety((Loop ? (points[0] - points[^1]) : (points[1] - points[0])).SafeNormalize(Vector2.Zero));
            var offset = normal * halfWidth;

            AddVertexPosition(ref vertexIndex, points[0] + offset);
            AddVertexPosition(ref vertexIndex, points[0] - offset);

            for (var i = 1; i < points.Count; i++)
            {
                normal = RotateClockwiseNinety((points[i] - points[i - 1]).SafeNormalize(Vector2.Zero));
                offset = normal * halfWidth;

                AddVertexPosition(ref vertexIndex, points[i] + offset);
                AddVertexPosition(ref vertexIndex, points[i] - offset);
            }

            if (!Loop) return;

            normal = RotateClockwiseNinety((points[0] - points[^1]).SafeNormalize(Vector2.Zero));
            offset = normal * halfWidth;

            AddVertexPosition(ref vertexIndex, points[0] + offset);
            AddVertexPosition(ref vertexIndex, points[0] - offset);
        }

        private void CalculateAdvancedVertexPositions()
        {
            var vertexIndex = 0;

            var topLines = new List<Line>();
            var bottomLines = new List<Line>();
            var segmentCount = Loop ? points.Count : points.Count - 1;

            for (var i = 0; i < segmentCount; i++)
            {
                var j = i == points.Count - 1 ? 0 : i + 1;

                var direction = Vector2.Normalize(points[j] - points[i]);

                var left = RotateClockwiseNinety(direction);
                var top = points[i] + halfWidth * left;
                var bottom = points[i] - halfWidth * left;

                topLines.Add(new Line { Offset = top, Direction = direction });
                bottomLines.Add(new Line { Offset = bottom, Direction = direction });
            }

            for (var i = 0; i < points.Count; i++)
            {
                var j = i == 0 ? points.Count - 1 : i - 1;

                if (i == 0 && !Loop)
                {
                    AddVertexPosition(ref vertexIndex, topLines[i].Offset);
                    AddVertexPosition(ref vertexIndex, bottomLines[i].Offset);
                }
                else if (i == points.Count - 1 && !Loop)
                {
                    var direction = topLines[j].Direction;

                    var left = RotateClockwiseNinety(direction);
                    var top = points[i] + halfWidth * left;
                    var bottom = points[i] - halfWidth * left;

                    AddVertexPosition(ref vertexIndex, top);
                    AddVertexPosition(ref vertexIndex, bottom);
                }
                else if (topLines[i].IsParallel(topLines[j]))
                {
                    AddVertexPosition(ref vertexIndex, topLines[i].Offset);
                    AddVertexPosition(ref vertexIndex, bottomLines[i].Offset);
                }
                else
                {
                    var topIntersection = topLines[i].Intersection(topLines[j]);
                    var bottomIntersection = bottomLines[i].Intersection(bottomLines[j]);

                    AddVertexPosition(ref vertexIndex, topIntersection);
                    AddVertexPosition(ref vertexIndex, bottomIntersection);
                }
            }

            if (Loop)
            {
                var topIntersection = topLines[^1].Intersection(topLines[0]);
                var bottomIntersection = bottomLines[^1].Intersection(bottomLines[0]);

                AddVertexPosition(ref vertexIndex, topIntersection);
                AddVertexPosition(ref vertexIndex, bottomIntersection);
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

        private static Vector2 RotateClockwiseNinety(Vector2 vector)
        {
            return new(-vector.Y, vector.X);
        }
    }
}