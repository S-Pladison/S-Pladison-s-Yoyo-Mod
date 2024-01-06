using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace SPYoyoMod.Common.Renderers
{
    public class PrimitiveRenderer
    {
        private readonly DynamicVertexBuffer vertexBuffer;
        private readonly DynamicIndexBuffer indexBuffer;

        public PrimitiveRenderer(int maxVertices, int maxIndices)
        {
            var device = Main.graphics.GraphicsDevice;

            vertexBuffer = new DynamicVertexBuffer(device, typeof(VertexPositionColorTexture), maxVertices, BufferUsage.None);
            indexBuffer = new DynamicIndexBuffer(device, IndexElementSize.SixteenBits, maxIndices, BufferUsage.None);
        }

        public void SetVertices(params VertexPositionColorTexture[] vertices)
        {
            vertexBuffer.SetData(0, vertices, 0, vertices.Length, VertexPositionColorTexture.VertexDeclaration.VertexStride, SetDataOptions.Discard);
        }

        public void SetIndices(params short[] indices)
        {
            indexBuffer.SetData(0, indices, 0, indices.Length, SetDataOptions.Discard);
        }

        public void Draw(Asset<Effect> effect)
        {
            Draw(effect.Value);
        }

        public void Draw(Effect effect)
        {
            var device = Main.graphics.GraphicsDevice;

            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexBuffer.VertexCount, 0, indexBuffer.IndexCount / 3);
            }
        }
    }
}