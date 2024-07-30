using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Utils;
using System;
using System.Runtime.CompilerServices;
using Terraria;

namespace SPYoyoMod.Common.Graphics.Renderers
{
    public sealed class StripRenderer : IRenderer, IDisposable
    {
        private DynamicVertexBuffer _vertexBuffer;
        private DynamicIndexBuffer _indexBuffer;
        private Vertex2DPositionColorTexture[] _vertices;
        private short[] _indices;

        private int _currentPointCapacity;
        private int _innerPointCapacity;
        private bool _isDirty;
        private bool _innerLoop;
        private float _innerStartWidth;
        private float _innerEndWidth;

        private readonly GraphicsDevice _device;
        private readonly FastList<Vector2> _innerPoints;

        public bool Loop
        {
            get => _innerLoop;
            set => SetLoop(value);
        }

        public float StartWidth
        {
            get => _innerStartWidth;
            set => SetStartWidth(value);
        }

        public float EndtWidth
        {
            get => _innerEndWidth;
            set => SetEndWidth(value);
        }

        public int PointCapacity
        {
            get => _innerPointCapacity;
            set => SetPointCapacity(value);
        }

        public int PointCount
        {
            get => _innerPoints.Length;
        }

        public bool IsDisposed
        {
            get;
            private set;
        }

        public StripRenderer(GraphicsDevice device, int capacity = 8)
        {
            _device = device;
            _vertices = [];
            _indices = [];
            _innerPoints = new();

            SetPointCapacity(capacity);
            SetStartEndWidth(16f, 16f);
            SetLoop(false);
        }

        public StripRenderer SetPointCapacity(int value)
        {
            if (_innerPointCapacity == value)
                return this;

            _innerPoints.Clear();
            _innerPointCapacity = value;
            _isDirty = true;

            return this;
        }

        public StripRenderer SetLoop(bool value)
        {
            if (_innerLoop == value)
                return this;

            _innerLoop = value;
            _isDirty = true;

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StripRenderer SetStartEndWidth(float start, float end)
            => SetStartWidth(start).SetEndWidth(end);

        public StripRenderer SetStartWidth(float value)
        {
            if (_innerStartWidth == value)
                return this;

            _innerStartWidth = value;
            _isDirty = true;

            return this;
        }

        public StripRenderer SetEndWidth(float value)
        {
            if (_innerEndWidth == value)
                return this;

            _innerEndWidth = value;
            _isDirty = true;

            return this;
        }

        public StripRenderer SetPoints(Vector2[] points)
        {
            _innerPoints.Reset();
            _innerPoints.EnsureCapacity(points.Length);

            for (var i = 0; i < points.Length; i++)
            {
                _innerPoints.Buffer[i] = points[i];
                _innerPoints.Length++;
            }

            _isDirty = true;

            return this;
        }

        public void Render()
        {
            if (IsDisposed || PointCount < 2)
                return;

            if (_isDirty)
            {
                Recalculate();

                _isDirty = false;
            }

            var segmentCount = PointCount + (Loop ? 0 : -1);
            var vertexCount = 2 * (segmentCount + 1);
            var indexCount = 6 * segmentCount;

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

            CalculateFactorsFromStartToEnd(out float[] factorsFromStartToEnd);
            CalculateVertexPositions(factorsFromStartToEnd);
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

        private void CalculateVertexIndices(int from, int to)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Add(ref int index, int value) => _indices[index++] = (short)value;

            for (var i = from; i < to; i++)
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

        private void CalculateVertexColors(int from, int to)
        {
            for (var i = from; i <= to; i++)
            {
                var index = i * 2;

                _vertices[index].Color = Color.White;
                _vertices[index + 1].Color = Color.White;
            }
        }

        private void CalculateFactorsFromStartToEnd(out float[] factorsFromStartToEnd)
        {
            var segmentCount = PointCount + (Loop ? 0 : -1);
            var accumulativeLength = 0f;
            var lengths = new float[segmentCount];
            var totalLength = 0f;

            factorsFromStartToEnd = new float[segmentCount];

            for (var i = 0; i < PointCount - 1; i++)
            {
                lengths[i] = Vector2.DistanceSquared(_innerPoints[i], _innerPoints[i + 1]);
                totalLength += lengths[i];
            }

            if (Loop)
            {
                lengths[^1] = Vector2.DistanceSquared(_innerPoints[_innerPoints.Length - 1], _innerPoints[0]);
                totalLength += lengths[^1];
            }

            for (var i = 0; i < segmentCount; i++)
            {
                accumulativeLength += lengths[i];
                factorsFromStartToEnd[i] = accumulativeLength / totalLength;
            }
        }

        private void CalculateVertexPositions(float[] factorsFromStartToEnd)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Vector2 RotateClockwiseNinety(Vector2 vector) => new(-vector.Y, vector.X);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            float GetWidth(float factor) => MathHelper.Lerp(_innerStartWidth, _innerEndWidth, factor);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AddVertexPosition(ref int vertexIndex, Vector2 position) => _vertices[vertexIndex++].Position = position;

            var vertexIndex = 0;
            var normal = RotateClockwiseNinety((Loop ? _innerPoints[0] - _innerPoints[_innerPoints.Length - 1] : _innerPoints[1] - _innerPoints[0]).SafeNormalize(Vector2.Zero));
            var halfWidth = GetWidth(0f) / 2f;
            var offset = normal * halfWidth;

            AddVertexPosition(ref vertexIndex, _innerPoints[0] + offset);
            AddVertexPosition(ref vertexIndex, _innerPoints[0] - offset);

            for (var i = 1; i < _innerPoints.Length; i++)
            {
                normal = RotateClockwiseNinety((_innerPoints[i] - _innerPoints[i - 1]).SafeNormalize(Vector2.Zero));
                halfWidth = GetWidth(factorsFromStartToEnd[i - 1]) / 2f;
                offset = normal * halfWidth;

                AddVertexPosition(ref vertexIndex, _innerPoints[i] + offset);
                AddVertexPosition(ref vertexIndex, _innerPoints[i] - offset);
            }

            if (Loop)
            {
                normal = RotateClockwiseNinety((_innerPoints[0] - _innerPoints[_innerPoints.Length - 1]).SafeNormalize(Vector2.Zero));
                halfWidth = GetWidth(1f) / 2f;
                offset = normal * halfWidth;

                AddVertexPosition(ref vertexIndex, _innerPoints[0] + offset);
                AddVertexPosition(ref vertexIndex, _innerPoints[0] - offset);
            }
        }

        private void CalculateVertexUVs(float[] factorsFromStartToEnd)
        {
            var vertexIndex = 0;

            _vertices[vertexIndex++].TextureCoordinate = Vector2.Zero;
            _vertices[vertexIndex++].TextureCoordinate = Vector2.UnitY;

            for (var i = 0; i < factorsFromStartToEnd.Length; i++)
            {
                _vertices[vertexIndex++].TextureCoordinate = new Vector2(factorsFromStartToEnd[i], 0);
                _vertices[vertexIndex++].TextureCoordinate = new Vector2(factorsFromStartToEnd[i], 1);
            }
        }
    }
}