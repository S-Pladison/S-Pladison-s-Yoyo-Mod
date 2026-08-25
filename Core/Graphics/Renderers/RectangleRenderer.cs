using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Utils;
using System;
using System.Runtime.CompilerServices;

namespace SPYoyoMod.Core.Graphics.Renderers
{
    /// <summary>
    /// Класс для создания и управления рендерингом прямоугольника.
    /// Форма задаётся центром, размером и углом поворота.
    /// </summary>
    public sealed class RectangleRenderer : IDisposable
    {
        private const int VertexCount = 4;
        private const int IndexCount = 6;

        private readonly GraphicsDevice _device;

        private DynamicVertexBuffer _vertexBuffer;
        private DynamicIndexBuffer _indexBuffer;
        private readonly Vertex2DPositionColorTexture[] _vertices;
        private readonly short[] _indices;

        private bool _isDirty;
        private Color _innerColor;
        private Vector2 _innerPosition;
        private Vector2 _innerSize;
        private float _innerRotation;

        /// <summary>
        /// Цвет прямоугольника.
        /// </summary>
        public Color Color
        {
            get => _innerColor;
            set => SetColor(value);
        }

        /// <summary>
        /// Позиция центра прямоугольника.
        /// </summary>
        public Vector2 Position
        {
            get => _innerPosition;
            set => SetPosition(value);
        }

        /// <summary>
        /// Ширина и высота прямоугольника.
        /// </summary>
        public Vector2 Size
        {
            get => _innerSize;
            set => SetSize(value);
        }

        /// <summary>
        /// Угол поворота прямоугольника в радианах относительно его центра.
        /// </summary>
        public float Rotation
        {
            get => _innerRotation;
            set => SetRotation(value);
        }

        /// <summary>
        /// Показывает, был ли освобожден объект и очищены его ресурсы.
        /// </summary>
        public bool IsDisposed
        {
            get;
            private set;
        }

        public RectangleRenderer(GraphicsDevice device)
        {
            _device = device;
            _vertices = new Vertex2DPositionColorTexture[VertexCount];
            _indices = new short[IndexCount];

            PrepareVertexIndices();
            PrepareVertexUVs();

            SetColor(Color.White);
            SetPosition(Vector2.Zero);
            SetSize(new Vector2(64f));
            SetRotation(0f);
        }

        /// <summary>
        /// Установить цвет прямоугольника.
        /// </summary>
        public RectangleRenderer SetColor(Color value)
        {
            if (_innerColor == value)
                return this;

            _innerColor = value;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Установить позицию центра прямоугольника.
        /// </summary>
        public RectangleRenderer SetPosition(Vector2 position)
        {
            if (_innerPosition == position)
                return this;

            if (!_isDirty)
                Offset(position - _innerPosition);

            _innerPosition = position;

            return this;
        }

        /// <summary>
        /// Установить ширину и высоту прямоугольника.
        /// </summary>
        public RectangleRenderer SetSize(Vector2 size)
        {
            size.X = Math.Max(size.X, 0f);
            size.Y = Math.Max(size.Y, 0f);

            if (_innerSize == size)
                return this;

            _innerSize = size;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Установить ширину и высоту прямоугольника.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectangleRenderer SetSize(float width, float height)
            => SetSize(new Vector2(width, height));

        /// <summary>
        /// Установить размер квадрата.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectangleRenderer SetSize(float size)
            => SetSize(new Vector2(size));

        /// <summary>
        /// Установить угол поворота прямоугольника относительно его центра.
        /// </summary>
        public RectangleRenderer SetRotation(float rotation)
        {
            if (_innerRotation == rotation)
                return this;

            _innerRotation = rotation;
            _isDirty = true;

            return this;
        }

        /// <summary>
        /// Выполняет отрисовку прямоугольника.
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

            _device.SetVertexBuffer(_vertexBuffer);
            _device.Indices = _indexBuffer;

            _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, VertexCount, 0, 2);
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
            if (_vertexBuffer is null)
            {
                _vertexBuffer = new(_device, typeof(Vertex2DPositionColorTexture), VertexCount, BufferUsage.WriteOnly);
                _indexBuffer = new(_device, IndexElementSize.SixteenBits, IndexCount, BufferUsage.WriteOnly);

                _indexBuffer.SetData(0, _indices, 0, _indices.Length, SetDataOptions.Discard);
            }

            PrepareVertexPositions();
            PrepareVertexColors();

            _vertexBuffer.SetData(0, _vertices, 0, _vertices.Length, Vertex2DPositionColorTexture.StaticVertexDeclaration.VertexStride, SetDataOptions.Discard);
        }

        private void PrepareVertexIndices()
        {
            _indices[0] = 0;
            _indices[1] = 1;
            _indices[2] = 2;
            _indices[3] = 2;
            _indices[4] = 3;
            _indices[5] = 0;
        }

        private void PrepareVertexPositions()
        {
            var cos = MathF.Cos(_innerRotation);
            var sin = MathF.Sin(_innerRotation);
            var halfX = _innerSize.X * 0.5f;
            var halfY = _innerSize.Y * 0.5f;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            Vector2 ToWorld(float x, float y)
            {
                return new Vector2(
                    _innerPosition.X + x * cos - y * sin,
                    _innerPosition.Y + x * sin + y * cos
                );
            }

            _vertices[0].Position = ToWorld(-halfX, -halfY);
            _vertices[1].Position = ToWorld(halfX, -halfY);
            _vertices[2].Position = ToWorld(halfX, halfY);
            _vertices[3].Position = ToWorld(-halfX, halfY);
        }

        private void PrepareVertexColors()
        {
            for (var i = 0; i < VertexCount; i++)
            {
                _vertices[i].Color = Color;
            }
        }

        private void PrepareVertexUVs()
        {
            _vertices[0].TextureCoordinate = new Vector2(0f, 0f);
            _vertices[1].TextureCoordinate = new Vector2(1f, 0f);
            _vertices[2].TextureCoordinate = new Vector2(1f, 1f);
            _vertices[3].TextureCoordinate = new Vector2(0f, 1f);
        }
    }
}
