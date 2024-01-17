using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;

namespace SPYoyoMod.Common.Renderers
{
    public class LineRenderer
    {
        private class Line
        {
            private static readonly float ParallelThreshold;

            static Line()
            {
                ParallelThreshold = 0.01f;
            }

            public Vector2 Offset;
            public Vector2 Direction;

            public Vector2 Evaluate(float t)
            {
                return Offset + Direction * t;
            }

            public bool IsParallel(Line other)
            {
                return Vector3.Cross(new Vector3(Vector2.Normalize(Direction), 0), new Vector3(Vector2.Normalize(other.Direction), 0)).Length() < ParallelThreshold;
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
                float v = (a1 * c0 - a0 * c1) / (a0 * b1 - a1 * b0);
                float t = (-c0 - b0 * v) / a0;

                return new Tuple<float, float>(t, v);
            }
        }

        public int PointCount { get; init; }

        public bool Loop
        {
            get => innerLoop;
            set
            {
                if (innerLoop == value) return;

                SetLoop(value);
            }
        }

        public float Width
        {
            get => innerWidth;
            set
            {
                if (innerWidth == value) return;

                SetWidth(value);
            }
        }

        private readonly PrimitiveRenderer renderer;
        private readonly VertexPositionColorTexture[] vertices;
        private readonly short[] indices;

        private IList<Vector2> points;
        private bool innerLoop;
        private float innerWidth;
        private float halfWidth;

        public LineRenderer(int pointCount, float width, bool loop)
        {
            PointCount = pointCount;

            var truePointCount = pointCount + (loop ? 1 : 0);

            renderer = new PrimitiveRenderer(2 * truePointCount, 6 * truePointCount - 6);
            vertices = new VertexPositionColorTexture[truePointCount * 2];
            indices = new short[truePointCount * 6 - 6];

            innerLoop = loop;
            innerWidth = width;
            halfWidth = width / 2f;

            CalculateVertexIndices();
            CalculateVertexColors();

            renderer.SetIndices(indices);
        }

        public LineRenderer SetPoints(IList<Vector2> points)
        {
            if (points.Count != PointCount)
                throw new ArgumentException($"Points count is not equal to lineRenderer.PointCount...");

            this.points = points;

            RecalculateMesh();
            return this;
        }

        public LineRenderer SetWidth(float width)
        {
            innerWidth = width;
            halfWidth = innerWidth / 2f;

            RecalculateMesh();
            return this;
        }

        public LineRenderer SetLoop(bool value)
        {
            innerLoop = value;

            RecalculateMesh();
            return this;
        }

        public LineRenderer Offset(Vector2 offset)
        {
            if (points is null)
                throw new NullReferenceException(nameof(points));

            for (int i = 0; i < points.Count; i++)
            {
                points[i] += offset;
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Position.X += offset.X;
                vertices[i].Position.Y += offset.Y;
            }

            renderer.SetVertices(vertices);

            return this;
        }

        private LineRenderer RecalculateMesh()
        {
            CalculateFactorsFromStartToEnd(out float[] factorsFromStartToEnd);
            CalculateVertexPositions();
            CalculateVertexUVs(factorsFromStartToEnd);

            renderer.SetVertices(vertices);

            return this;
        }

        public void Draw(Asset<Effect> effect)
        {
            Draw(effect.Value);
        }

        public void Draw(Effect effect)
        {
            if (points is null)
                throw new NullReferenceException(nameof(points));

            renderer.Draw(effect);
        }

        private void CalculateFactorsFromStartToEnd(out float[] factorsFromStartToEnd)
        {
            var segmentCount = Loop ? points.Count : (points.Count - 1);
            var accumulativeLength = 0f;
            var lengths = new float[segmentCount];
            var totalLength = 0f;

            factorsFromStartToEnd = new float[segmentCount];

            for (int i = 0; i < points.Count - 1; i++)
            {
                lengths[i] = Vector2.Distance(points[i], points[i + 1]);
                totalLength += lengths[i];
            }

            if (Loop)
            {
                lengths[^1] = Vector2.Distance(points[^1], points[0]);
                totalLength += lengths[^1];
            }

            for (int i = 0; i < segmentCount; i++)
            {
                accumulativeLength += lengths[i];
                factorsFromStartToEnd[i] = accumulativeLength / totalLength;
            }
        }

        private void CalculateVertexPositions()
        {
            var vertexIndex = 0;
            var topLines = new List<Line>();
            var bottomLines = new List<Line>();
            var segmentCount = Loop ? points.Count : (points.Count - 1);

            for (int i = 0; i < segmentCount; i++)
            {
                int j = (i + 1) % segmentCount;

                var direction = Vector2.Normalize(points[j] - points[i]);

                var left = RotateClockwiseNinety(direction);
                var top = points[i] + halfWidth * left;
                var bottom = points[i] - halfWidth * left;

                topLines.Add(new Line { Offset = top, Direction = direction });
                bottomLines.Add(new Line { Offset = bottom, Direction = direction });
            }

            for (int i = 0; i < points.Count; i++)
            {
                int j = (i == 0) ? points.Count - 1 : i - 1;

                if (i == 0 && !Loop)
                {
                    AddVertexPosition(ref vertexIndex, topLines[i].Offset);
                    AddVertexPosition(ref vertexIndex, bottomLines[i].Offset);
                }
                else if ((i == points.Count - 1) && !Loop)
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

        private void CalculateVertexIndices()
        {
            var indices = new List<int>();
            var segmentCount = Loop ? PointCount : (PointCount - 1);

            for (int i = 0; i < segmentCount; i++)
            {
                int i2 = i * 2;
                int j2 = (i + 1) * 2;

                indices.AddRange(new int[] { i2, i2 + 1, j2 + 1, j2 + 1, j2, i2 });
            }

            for (int i = 0; i < this.indices.Length; i++)
            {
                this.indices[i] = (short)indices[i];
            }
        }

        private void CalculateVertexUVs(float[] factorsFromStartToEnd)
        {
            var vertexIndex = 0;

            AddVertexUV(ref vertexIndex, Vector2.Zero);
            AddVertexUV(ref vertexIndex, Vector2.UnitY);

            for (int i = 0; i < factorsFromStartToEnd.Length; i++)
            {
                AddVertexUV(ref vertexIndex, new Vector2(factorsFromStartToEnd[i], 0));
                AddVertexUV(ref vertexIndex, new Vector2(factorsFromStartToEnd[i], 1));
            }
        }

        private void AddVertexUV(ref int vertexIndex, Vector2 uv)
        {
            vertices[vertexIndex++].TextureCoordinate = uv;
        }

        private void CalculateVertexColors()
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Color = Color.White;
            }
        }

        private static Vector2 RotateClockwiseNinety(Vector2 vector)
        {
            return new(-vector.Y, vector.X);
        }
    }
}