using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace SPYoyoMod.Common.Graphics
{
    public class RingRenderer : IDisposable
    {
        public const int MinPointCount = 3;

        private PrimitiveRenderer _renderer;
        private Vertex2DPositionColorTexture[] _vertices;
        private short[] _indices;

        private bool _isDirty;
        private int _maxPointCount;
        private int _innerPointCount;
        private float _innerRadius;
        private float _innerThickness;
        private float _halfThickness;
        private Vector2 _innerPosition;

        public int PointCount
        {
            get => _innerPointCount;
            set => SetPointCount(value);
        }

        public Vector2 Position
        {
            get => _innerPosition;
            set => SetPosition(value);
        }

        public float Thickness
        {
            get => _innerThickness;
            set => SetThickness(value);
        }

        public float Radius
        {
            get => _innerRadius;
            set => SetRadius(value);
        }

        public RingRenderer()
        {
            _vertices = [];
            _indices = [];

            SetPointCount(MinPointCount);
            SetPosition(Vector2.Zero);
            SetThickness(8);
            SetRadius(64);

            Recalculate();
        }

        public RingRenderer SetPointCount(int pointCount)
        {
            pointCount = Math.Max(pointCount, MinPointCount);

            if (_innerPointCount == pointCount)
                return this;

            _innerPointCount = pointCount;
            _isDirty = true;

            return this;
        }

        public RingRenderer SetPosition(Vector2 position)
        {
            if (_innerPosition == position)
                return this;

            Offset(position - _innerPosition);
            _innerPosition = position;

            return this;
        }

        public RingRenderer SetThickness(float thickness)
        {
            if (_innerRadius == thickness)
                return this;

            _innerThickness = thickness;
            _halfThickness = thickness / 2f;
            _isDirty = true;

            return this;
        }

        public RingRenderer SetRadius(float radius)
        {
            if (_innerRadius == radius)
                return this;

            _innerRadius = radius;
            _isDirty = true;

            return this;
        }

        public void Draw()
        {
            if (_isDirty)
            {
                Recalculate();

                _isDirty = false;
            }

            var vertexCount = 2 * (PointCount + 1);
            var indexCount = 6 * PointCount;

            _renderer.Draw(PrimitiveType.TriangleList, 0, 0, vertexCount, 0, indexCount / 3);
        }

        public void Dispose()
        {
            _renderer?.Dispose();
        }

        private void Offset(Vector2 offset)
        {
            for (var i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].Position.X += offset.X;
                _vertices[i].Position.Y += offset.Y;
            }

            _renderer.SetVertices(_vertices);
        }

        private void Recalculate()
        {
            if (_maxPointCount < PointCount)
            {
                var oldMaxPointCount = _maxPointCount;

                _maxPointCount = PointCount;

                var maxVertices = 2 * (_maxPointCount + 1);
                var maxIndices = 6 * _maxPointCount;

                _renderer = new PrimitiveRenderer(Main.graphics.GraphicsDevice, maxVertices, maxIndices);

                Array.Resize(ref _vertices, maxVertices);
                Array.Resize(ref _indices, maxIndices);

                CalculateVertexIndices(oldMaxPointCount, _maxPointCount);
                CalculateVertexColors(oldMaxPointCount, _maxPointCount);

                _renderer.SetIndices(_indices);
            }

            CalculateVertexPositions(out Vector2[] points);
            CalculateFactorsFromStartToEnd(points, out float[] factorsFromStartToEnd);
            CalculateVertexUVs(factorsFromStartToEnd);

            _renderer.SetVertices(_vertices);
        }

        private void CalculateVertexIndices(int start, int end)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Add(ref int index, int value)
            {
                _indices[index++] = (short)value;
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

                _vertices[index].Color = Color.White;
                _vertices[index + 1].Color = Color.White;
            }
        }

        private void CalculateVertexPositions(out Vector2[] points)
        {
            var vertexIndex = 0;
            var step = MathHelper.TwoPi / PointCount;
            points = new Vector2[PointCount + 1];

            for (var i = 0; i <= PointCount; i++)
            {
                var angle = step * i;
                var direction = Vector2.UnitX.RotatedBy(angle);
                var pointPosition = Position + direction * Radius;
                var offset = direction * _halfThickness;

                points[i] = pointPosition;

                _vertices[vertexIndex++].Position = pointPosition - offset;
                _vertices[vertexIndex++].Position = pointPosition + offset;
            }
        }

        private void CalculateVertexUVs(float[] factorsFromStartToEnd)
        {
            var vertexIndex = 0;

            for (var i = 0; i < factorsFromStartToEnd.Length; i++)
            {
                _vertices[vertexIndex++].TextureCoordinate = new Vector2(factorsFromStartToEnd[i], 0);
                _vertices[vertexIndex++].TextureCoordinate = new Vector2(factorsFromStartToEnd[i], 1);
            }
        }

        private void CalculateFactorsFromStartToEnd(Vector2[] points, out float[] factorsFromStartToEnd)
        {
            var accumulativeLength = 0f;
            var lengths = new float[PointCount];
            var totalLength = 0f;

            factorsFromStartToEnd = new float[PointCount + 1];

            for (var i = 0; i < PointCount; i++)
            {
                var j = (i + 1) % PointCount;

                lengths[i] = Vector2.DistanceSquared(points[i], points[j]);
                totalLength += lengths[i];
            }

            for (var i = 0; i < PointCount; i++)
            {
                accumulativeLength += lengths[i];
                factorsFromStartToEnd[i + 1] = accumulativeLength / totalLength;
            }
        }
    }
}