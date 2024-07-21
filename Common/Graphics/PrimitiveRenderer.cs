using System;
using Microsoft.Xna.Framework.Graphics;

namespace SPYoyoMod.Common.Graphics
{
    public class PrimitiveRenderer(GraphicsDevice device, int maxVertices, int maxIndices) : IDisposable
    {
        private readonly DynamicVertexBuffer _vertexBuffer = new(device, typeof(Vertex2DPositionColorTexture), maxVertices, BufferUsage.WriteOnly);
        private readonly DynamicIndexBuffer _indexBuffer = new(device, IndexElementSize.SixteenBits, maxIndices, BufferUsage.WriteOnly);

        public bool IsDisposed { get; private set; }

        public void SetVertices(Vertex2DPositionColorTexture[] vertices)
        {
            _vertexBuffer?.SetData(0, vertices, 0, vertices.Length, Vertex2DPositionColorTexture.StaticVertexDeclaration.VertexStride, SetDataOptions.Discard);
        }

        public void SetIndices(short[] indices)
        {
            _indexBuffer?.SetData(0, indices, 0, indices.Length, SetDataOptions.Discard);
        }

        public void Draw(PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount)
        {
            if (IsDisposed || _vertexBuffer is null || _indexBuffer is null)
                return;

            device.SetVertexBuffer(_vertexBuffer);
            device.Indices = _indexBuffer;

            device.DrawIndexedPrimitives(primitiveType, baseVertex, minVertexIndex, numVertices, startIndex, primitiveCount);
        }

        public void Dispose()
        {
            IsDisposed = true;

            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
        }
    }
}