using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;

namespace SPYoyoMod.Core.Graphics.Renderers
{
    /// <summary>
    /// Класс для создания и управления рендерингом ленты (трейла), основанной на последовательности точек.
    /// </summary>
    public sealed class StripRenderer : IDisposable
    {
        /// <summary>
        /// Минимальное количество точек, необходимых для построения ленты.
        /// </summary>
        public const int MinPointCount = 2;

        private readonly GraphicsDevice _device;
        private readonly FastList<Vector2> _innerPoints;

        private DynamicVertexBuffer _vertexBuffer;
        private DynamicIndexBuffer _indexBuffer;
        private Vertex2DPositionColorTexture[] _vertices;
        private short[] _indices;

        private bool _isDirty;
        private bool _innerLoop;
        private bool _innerIgnoreDefaultPoints;
        private float _innerStartWidth;
        private float _innerEndWidth;
        private Color _innerStartColor;
        private Color _innerEndColor;
        private int _innerPointCapacity;
        private int _currentPointCapacity;

        /// <summary>
        /// Зациклена ли лента. По умолчанию установлено false.
        /// </summary>
        public bool Loop
        {
            get => _innerLoop;
            set => SetLoop(value);
        }

        /// <summary>
        /// Следует ли игнорировать стандартные точки при построении ленты. По умолчанию false.
        /// </summary>
        public bool IgnoreDefaultPoints
        {
            get => _innerIgnoreDefaultPoints;
            set => SetIgnoreDefaultPoints(value);
        }

        /// <summary>
        /// Стартовая ширина ленты.
        /// </summary>
        public float StartWidth
        {
            get => _innerStartWidth;
            set => SetStartWidth(value);
        }

        /// <summary>
        /// Конечная ширина ленты.
        /// </summary>
        public float EndWidth
        {
            get => _innerEndWidth;
            set => SetEndWidth(value);
        }

        /// <summary>
        /// Начальный цвет ленты.
        /// </summary>
        public Color StartColor
        {
            get => _innerStartColor;
            set => SetStartColor(value);
        }

        /// <summary>
        /// Конечный цвет ленты.
        /// </summary>
        public Color EndColor
        {
            get => _innerEndColor;
            set => SetEndColor(value);
        }

        /// <summary>
        /// Максимальное количество точек, которые может хранить отрисовщик ленты.
        /// При изменении/превышении значения, все хранящиеся точки будут стерты, а максимальное кол-во точек будет увеличено.
        /// </summary>
        public int PointCapacity
        {
            get => _innerPointCapacity;
            set => SetPointCapacity(value);
        }

        /// <summary>
        /// Точки, используемые для построения ленты. Каждая точка определяет положение, через которое проходит полоса, создавая её итоговую форму.
        /// </summary>
        public IReadOnlyList<Vector2> Points
        {
            get => _innerPoints.Buffer;
            set => SetPoints(value);
        }

        /// <summary>
        /// Количество точек, содержащихся в рендерере ленты.
        /// </summary>
        public int PointCount
        {
            get => _innerPoints.Length;
        }

        /// <summary>
        /// Показывает, был ли освобожден объект и очищены его ресурсы.
        /// </summary>
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

            SetLoop(false);
            SetIgnoreDefaultPoints(false);
            SetWidth(16f);
            SetColor(Color.White);
            SetPointCapacity(capacity);
        }

        /// <summary>
        /// Установить значение зацикленности. По умолчанию установлено false.
        /// </summary>
        public StripRenderer SetLoop(bool value)
        {
            if (_innerLoop == value)
                return this;

            _innerLoop = value;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Устанавить параметр, указывающий, следует ли игнорировать стандартные точки при построении ленты.
        /// </summary>
        public StripRenderer SetIgnoreDefaultPoints(bool value)
        {
            if (_innerIgnoreDefaultPoints == value)
                return this;

            _innerIgnoreDefaultPoints = value;
            return this;
        }

        /// <summary>
        /// Установить общее значение ширины ленты.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StripRenderer SetWidth(float value)
            => SetStartWidth(value).SetEndWidth(value);

        /// <summary>
        /// Установить значения ширины ленты.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StripRenderer SetStartEndWidth(float start, float end)
            => SetStartWidth(start).SetEndWidth(end);

        /// <summary>
        /// Установить стартовое значение ширины ленты.
        /// </summary>
        public StripRenderer SetStartWidth(float value)
        {
            if (_innerStartWidth == value)
                return this;

            _innerStartWidth = value;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Установить конечное значение ширины ленты.
        /// </summary>
        public StripRenderer SetEndWidth(float value)
        {
            if (_innerEndWidth == value)
                return this;

            _innerEndWidth = value;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Установить общее значение цвета ленты.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StripRenderer SetColor(Color value)
            => SetStartColor(value).SetEndColor(value);

        /// <summary>
        /// Установить значения цвета ленты.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StripRenderer SetStartEndColor(Color start, Color end)
            => SetStartColor(start).SetEndColor(end);

        /// <summary>
        /// Установить начальный цвет ленты.
        /// </summary>
        public StripRenderer SetStartColor(Color value)
        {
            if (_innerStartColor == value)
                return this;

            _innerStartColor = value;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Установить конечный цвет ленты.
        /// </summary>
        public StripRenderer SetEndColor(Color value)
        {
            if (_innerEndColor == value)
                return this;

            _innerEndColor = value;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Установить максимальное количество точек, которые может хранить отрисовщик ленты.
        /// При изменении/превышении значения, все хранящиеся точки будут стерты, а максимальное кол-во точек будет увеличено.
        /// </summary>
        public StripRenderer SetPointCapacity(int value)
        {
            if (_innerPointCapacity == value)
                return this;

            _innerPoints.Clear();
            _innerPointCapacity = value;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Установить последовательность точек, используемых для построения ленты.
        /// </summary>
        public StripRenderer SetPoints(IReadOnlyCollection<Vector2> points)
        {
            _innerPoints.Reset();
            _innerPoints.EnsureCapacity(points.Count);

            if (!_innerIgnoreDefaultPoints)
            {
                foreach (var point in points)
                {
                    _innerPoints.Buffer[_innerPoints.Length] = point;
                    _innerPoints.Length++;
                }
            }
            else
            {
                foreach (var point in points)
                {
                    if (point == default)
                        continue;

                    _innerPoints.Buffer[_innerPoints.Length] = point;
                    _innerPoints.Length++;
                }
            }

            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Выполняет отрисовку ленты с использованием текущего набора точек.
        /// </summary>
        public void Render()
        {
            if (IsDisposed || PointCount < MinPointCount)
                return;

            if (_isDirty)
            {
                PrepareBuffers();
                _isDirty = false;
            }

            var segmentCount = PointCount + (Loop ? 0 : -1);
            var vertexCount = 2 * (segmentCount + 1);
            var indexCount = 6 * segmentCount;

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

            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();

            GC.SuppressFinalize(this);

            IsDisposed = true;
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

            PrepareFactorsFromStartToEnd(out float[] factorsFromStartToEnd);
            PrepareVertexPositions(factorsFromStartToEnd);
            PrepareVertexColors(factorsFromStartToEnd);
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

        private void PrepareVertexIndices(int from, int to)
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

        private void PrepareFactorsFromStartToEnd(out float[] factorsFromStartToEnd)
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

        private void PrepareVertexPositions(float[] factorsFromStartToEnd)
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

        private void PrepareVertexColors(float[] factorsFromStartToEnd)
        {
            var vertexIndex = 0;

            _vertices[vertexIndex++].Color = StartColor;
            _vertices[vertexIndex++].Color = StartColor;

            for (var i = 0; i < factorsFromStartToEnd.Length; i++)
            {
                var color = Color.Lerp(StartColor, EndColor, factorsFromStartToEnd[i]);

                _vertices[vertexIndex++].Color = color;
                _vertices[vertexIndex++].Color = color;
            }
        }

        private void PrepareVertexUVs(float[] factorsFromStartToEnd)
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