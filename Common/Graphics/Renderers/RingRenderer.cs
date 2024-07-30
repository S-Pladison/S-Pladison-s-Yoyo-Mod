using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace SPYoyoMod.Common.Graphics.Renderers
{
    public sealed class RingRenderer : IRenderer, IDisposable
    {
        public const int MinPointCount = 3;

        private DynamicVertexBuffer _vertexBuffer;
        private DynamicIndexBuffer _indexBuffer;
        private Vertex2DPositionColorTexture[] _vertices;
        private short[] _indices;

        private bool _isDirty;
        private int _currentPointCapacity;
        private int _innerPointCapacity;
        private int _innerPointCount;
        private float _innerRadius;
        private float _innerThickness;
        private float _halfThickness;
        private Vector2 _innerPosition;

        private readonly GraphicsDevice _device;

        public int PointCount
        {
            get => _innerPointCount;
            set => SetPointCount(value);
        }

        public int PointCapacity
        {
            get => _innerPointCapacity;
            set => SetPointCapacity(value);
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

        public bool IsDisposed
        {
            get;
            private set;
        }

        public RingRenderer(GraphicsDevice device, int capacity = 8)
        {
            _device = device;
            _vertices = [];
            _indices = [];

            SetPointCapacity(capacity);
            SetPointCount(MinPointCount);
            SetPosition(Vector2.Zero);
            SetThickness(8);
            SetRadius(64);
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

        public RingRenderer SetPointCapacity(int value)
        {
            if (_innerPointCapacity == value)
                return this;

            _innerPointCapacity = value;
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

        public void Render()
        {
            if (IsDisposed)
                return;

            if (_isDirty)
            {
                Recalculate();

                _isDirty = false;
            }

            var vertexCount = 2 * (PointCount + 1);
            var indexCount = 6 * PointCount;

            _device.SetVertexBuffer(_vertexBuffer);
            _device.Indices = _indexBuffer;

            _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexCount, 0, indexCount / 3);
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
        }

        private void Offset(Vector2 offset)
        {
            for (var i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].Position.X += offset.X;
                _vertices[i].Position.Y += offset.Y;
            }

            _vertexBuffer?.SetData(0, _vertices, 0, _vertices.Length, Vertex2DPositionColorTexture.StaticVertexDeclaration.VertexStride, SetDataOptions.Discard);
        }

        private void Recalculate()
        {
            while (PointCount > _innerPointCapacity)
                _innerPointCapacity = (int)(_innerPointCapacity * 1.5f);

            if (_currentPointCapacity < _innerPointCapacity)
            {
                ResizeBuffers(vertices: 2 * (_innerPointCapacity + 1), indices: 6 * _innerPointCapacity);
                CalculateVertexIndices(_currentPointCapacity, _innerPointCapacity);
                CalculateVertexColors(_currentPointCapacity, _innerPointCapacity);

                _indexBuffer.SetData(0, _indices, 0, _indices.Length, SetDataOptions.Discard);
                _currentPointCapacity = _innerPointCapacity;
            }

            CalculateVertexPositions(out Vector2[] points);
            CalculateFactorsFromStartToEnd(points, out float[] factorsFromStartToEnd);
            CalculateVertexUVs(factorsFromStartToEnd);

            _vertexBuffer.SetData(0, _vertices, 0, _vertices.Length, Vertex2DPositionColorTexture.StaticVertexDeclaration.VertexStride, SetDataOptions.Discard);
        }

        private void ResizeBuffers(int vertices, int indices)
        {
            _vertexBuffer?.Dispose();
            _vertexBuffer = new(Main.graphics.GraphicsDevice, typeof(Vertex2DPositionColorTexture), vertices, BufferUsage.WriteOnly);

            _indexBuffer?.Dispose();
            _indexBuffer = new(Main.graphics.GraphicsDevice, IndexElementSize.SixteenBits, indices, BufferUsage.WriteOnly);

            Array.Resize(ref _vertices, vertices);
            Array.Resize(ref _indices, indices);
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