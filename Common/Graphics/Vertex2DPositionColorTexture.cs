using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SPYoyoMod.Common.Graphics
{
    public struct Vertex2DPositionColorTexture(Vector2 position, Color color, Vector2 textureCoordinate) : IVertexType
    {
        public static readonly VertexDeclaration StaticVertexDeclaration = new(
        [
            new(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        ]);

        public Vector2 Position = position;
        public Color Color = color;
        public Vector2 TextureCoordinate = textureCoordinate;

        public readonly VertexDeclaration VertexDeclaration => StaticVertexDeclaration;
    }
}