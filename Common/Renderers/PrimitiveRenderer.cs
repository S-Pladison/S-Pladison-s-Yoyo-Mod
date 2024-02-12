using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;

namespace SPYoyoMod.Common.Renderers
{
    public class PrimitiveRenderer : IDisposable
    {
        private readonly DynamicVertexBuffer vertexBuffer;
        private readonly DynamicIndexBuffer indexBuffer;

        private bool isDisposed;

        public PrimitiveRenderer(int maxVertices, int maxIndices)
        {
            var device = Main.graphics.GraphicsDevice;

            vertexBuffer = new DynamicVertexBuffer(device, typeof(VertexPositionColorTexture), maxVertices, BufferUsage.None);
            indexBuffer = new DynamicIndexBuffer(device, IndexElementSize.SixteenBits, maxIndices, BufferUsage.None);
        }

        public void SetVertices(params VertexPositionColorTexture[] vertices)
        {
            if (isDisposed) return;

            vertexBuffer.SetData(0, vertices, 0, vertices.Length, VertexPositionColorTexture.VertexDeclaration.VertexStride, SetDataOptions.Discard);
        }

        public void SetIndices(params short[] indices)
        {
            if (isDisposed) return;

            indexBuffer.SetData(0, indices, 0, indices.Length, SetDataOptions.Discard);
        }

        public void Draw(Asset<Effect> effect, int vertexCount, int primitiveCount)
        {
            Draw(effect.Value, vertexCount, primitiveCount);
        }

        public void Draw(Effect effect, int vertexCount, int primitiveCount)
        {
            if (isDisposed) return;

            var device = Main.graphics.GraphicsDevice;

            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexCount, 0, primitiveCount);
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                vertexBuffer?.Dispose();
                indexBuffer?.Dispose();

                isDisposed = true;
            }
        }
    }
}