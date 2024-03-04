using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SPYoyoMod.Common.Graphics
{
    public struct Vertex2DPositionColorTexture : IVertexType
    {
        public static readonly VertexDeclaration StaticVertexDeclaration = new(new VertexElement[]
        {
            new(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        });

        public Vector2 Position;
        public Color Color;
        public Vector2 TextureCoordinate;

        public VertexDeclaration VertexDeclaration => StaticVertexDeclaration;

        public Vertex2DPositionColorTexture(Vector2 position, Color color, Vector2 textureCoordinate)
        {
            Position = position;
            Color = color;
            TextureCoordinate = textureCoordinate;
        }
    }
}