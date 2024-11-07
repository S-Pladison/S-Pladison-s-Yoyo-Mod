using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using Terraria;

namespace SPYoyoMod.Core.Graphics.Renderers
{
    /// <summary>
    /// Класс для создания и управления рендерингом кольца.
    /// </summary>
    public sealed class RingRenderer : IDisposable
    {
        /// <summary>
        /// Минимальное количество точек, необходимых для построения кольца.
        /// </summary>
        public const int MinPointCount = 3;

        private readonly GraphicsDevice _device;

        private DynamicVertexBuffer _vertexBuffer;
        private DynamicIndexBuffer _indexBuffer;
        private Vertex2DPositionColorTexture[] _vertices;
        private short[] _indices;

        private bool _isDirty;
        private Color _innerColor;
        private int _currentPointCapacity;
        private int _innerPointCapacity;
        private int _innerPointCount;
        private float _innerRadius;
        private float _innerThickness;
        private float _halfThickness;
        private Vector2 _innerPosition;

        /// <summary>
        /// Цвет кольца.
        /// </summary>
        public Color Color
        {
            get => _innerColor;
            set => SetColor(value);
        }

        /// <summary>
        /// Текущее количество точек, используемых для построения кольца.
        /// Определяет степень детализации кольца, где большее количество точек создаёт более плавную форму.
        /// </summary>
        public int PointCount
        {
            get => _innerPointCount;
            set => SetPointCount(value);
        }

        /// <summary>
        /// Максимальное количество точек, которые может содержать рендерер кольца для его отрисовки.
        /// Когда количество добавленных точек достигает этого значения, значение увеличивается, а в месте с ним и размеры буфферов.
        /// </summary>
        public int PointCapacity
        {
            get => _innerPointCapacity;
            set => SetPointCapacity(value);
        }

        /// <summary>
        /// Позиция центра кольца.
        /// </summary>
        public Vector2 Position
        {
            get => _innerPosition;
            set => SetPosition(value);
        }

        /// <summary>
        /// Толщина кольца.
        /// </summary>
        public float Thickness
        {
            get => _innerThickness;
            set => SetThickness(value);
        }

        /// <summary>
        /// Радиус кольца, определяющий его размер от центра до внешнего края.
        /// </summary>
        public float Radius
        {
            get => _innerRadius;
            set => SetRadius(value);
        }

        /// <summary>
        /// Показывает, был ли освобожден объект и очищены его ресурсы.
        /// </summary>
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

            SetColor(Color.White);
            SetPointCapacity(capacity);
            SetPointCount(MinPointCount);
            SetPosition(Vector2.Zero);
            SetThickness(8);
            SetRadius(64);
        }

        /// <summary>
        /// Установить цвет кольца.
        /// </summary>
        public RingRenderer SetColor(Color value)
        {
            if (_innerColor == value)
                return this;

            _innerColor = value;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Установить количество точек, используемых для построения кольца.
        /// Определяет степень детализации кольца, где большее количество точек создаёт более плавную форму.
        /// </summary>
        public RingRenderer SetPointCount(int pointCount)
        {
            pointCount = Math.Max(pointCount, MinPointCount);

            if (_innerPointCount == pointCount)
                return this;

            _innerPointCount = pointCount;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Установить максимальное количество точек, которые может содержать рендерер кольца для его отрисовки.
        /// Когда количество добавленных точек достигает этого значения, значение увеличивается, а в месте с ним и размеры буфферов.
        /// </summary>
        public RingRenderer SetPointCapacity(int value)
        {
            if (_innerPointCapacity == value)
                return this;

            _innerPointCapacity = value;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Установить позицию центра кольца.
        /// </summary>
        public RingRenderer SetPosition(Vector2 position)
        {
            if (_innerPosition == position)
                return this;

            if (!_isDirty)
                Offset(position - _innerPosition);

            _innerPosition = position;

            return this;
        }

        /// <summary>
        /// Установить толщину кольца.
        /// </summary>
        public RingRenderer SetThickness(float thickness)
        {
            if (_innerRadius == thickness)
                return this;

            _innerThickness = thickness;
            _halfThickness = thickness / 2f;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Установить радиус кольца, определяющий его размер от центра до внешнего края.
        /// </summary>
        public RingRenderer SetRadius(float radius)
        {
            if (_innerRadius == radius)
                return this;

            _innerRadius = radius;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Выполняет отрисовку кольца.
        /// </summary>
        public void Render()
        {
            if (IsDisposed)
                return;

            if (_isDirty)
            {
                PrepareBuffers();
                _isDirty = false;
            }

            var vertexCount = 2 * (PointCount + 1);
            var indexCount = 6 * PointCount;

            _device.SetVertexBuffer(_vertexBuffer);
            _device.Indices = _indexBuffer;

            _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexCount, 0, indexCount / 3);
        }

        /// <summary>
        /// Освобождение всех используемых ресурсов.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();

            GC.SuppressFinalize(this);
        }

        private void Offset(Vector2 value)
        {
            for (var i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].Position.X += value.X;
                _vertices[i].Position.Y += value.Y;
            }

            _vertexBuffer?.SetData(0, _vertices, 0, _vertices.Length, Vertex2DPositionColorTexture.StaticVertexDeclaration.VertexStride, SetDataOptions.Discard);
        }

        private void PrepareBuffers()
        {
            while (PointCount > _innerPointCapacity)
                _innerPointCapacity = (int)(_innerPointCapacity * 1.5f);

            if (_currentPointCapacity < _innerPointCapacity)
            {
                ResizeBuffers(vertices: 2 * (_innerPointCapacity + 1), indices: 6 * _innerPointCapacity);

                PrepareVertexIndices(_currentPointCapacity, _innerPointCapacity);

                _indexBuffer.SetData(0, _indices, 0, _indices.Length, SetDataOptions.Discard);
                _currentPointCapacity = _innerPointCapacity;
            }

            PrepareVertexPositions(out Vector2[] points);
            PrepareFactorsFromStartToEnd(points, out float[] factorsFromStartToEnd);
            PrepareVertexColors();
            PrepareVertexUVs(factorsFromStartToEnd);

            _vertexBuffer.SetData(0, _vertices, 0, _vertices.Length, Vertex2DPositionColorTexture.StaticVertexDeclaration.VertexStride, SetDataOptions.Discard);
        }

        private void ResizeBuffers(int vertices, int indices)
        {
            _vertexBuffer?.Dispose();
            _vertexBuffer = new(_device, typeof(Vertex2DPositionColorTexture), vertices, BufferUsage.WriteOnly);

            _indexBuffer?.Dispose();
            _indexBuffer = new(_device, IndexElementSize.SixteenBits, indices, BufferUsage.WriteOnly);

            Array.Resize(ref _vertices, vertices);
            Array.Resize(ref _indices, indices);
        }

        private void PrepareVertexIndices(int start, int end)
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

        private void PrepareFactorsFromStartToEnd(Vector2[] points, out float[] factorsFromStartToEnd)
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

        private void PrepareVertexPositions(out Vector2[] points)
        {
            var vertexIndex = 0;
            var step = MathHelper.TwoPi / PointCount;
            points = new Vector2[PointCount + 1];

            for (var i = 0; i <= PointCount; i++)
            {
                var angle = step * i;
                var direction = Vector2.UnitX.RotatedBy(angle);
                var pointPosition = Position + direction * (Radius - _halfThickness);
                var offset = direction * _halfThickness;

                points[i] = pointPosition;

                _vertices[vertexIndex++].Position = pointPosition - offset;
                _vertices[vertexIndex++].Position = pointPosition + offset;
            }
        }

        private void PrepareVertexColors()
        {
            var vertexCount = 2 * (PointCount + 1);

            for (var i = 0; i < vertexCount; i++)
            {
                _vertices[i].Color = Color;
            }
        }

        private void PrepareVertexUVs(float[] factorsFromStartToEnd)
        {
            var vertexIndex = 0;

            for (var i = 0; i < factorsFromStartToEnd.Length; i++)
            {
                _vertices[vertexIndex++].TextureCoordinate = new Vector2(factorsFromStartToEnd[i], 0);
                _vertices[vertexIndex++].TextureCoordinate = new Vector2(factorsFromStartToEnd[i], 1);
            }
        }
    }
}