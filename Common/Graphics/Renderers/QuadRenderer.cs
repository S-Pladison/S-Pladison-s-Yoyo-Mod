using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;

namespace SPYoyoMod.Common.Graphics.Renderers
{
    public class QuadRenderer : IDisposable
    {
        private readonly PrimitiveRenderer renderer;
        private readonly Vertex2DPositionColorTexture[] vertices;

        private bool isDirty;

        public QuadRenderer()
        {
            vertices = new Vertex2DPositionColorTexture[4];
            vertices[0].TextureCoordinate = Vector2.Zero;
            vertices[1].TextureCoordinate = Vector2.UnitX;
            vertices[2].TextureCoordinate = Vector2.One;
            vertices[3].TextureCoordinate = Vector2.UnitY;

            renderer = new PrimitiveRenderer(4, 6);
            renderer.SetIndices(new short[] { 0, 1, 3, 1, 2, 3 });

            SetColor(Color.White);
        }

        public QuadRenderer SetPoints(Rectangle rectangle)
        {
            return SetPoints(rectangle.TopLeft(), rectangle.TopRight(), rectangle.BottomRight(), rectangle.BottomLeft());
        }

        public QuadRenderer SetPoints(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            vertices[0].Position = a;
            vertices[1].Position = b;
            vertices[2].Position = c;
            vertices[3].Position = d;

            isDirty = true;

            return this;
        }

        public QuadRenderer SetColor(Color abcd)
        {
            return SetColors(abcd, abcd, abcd, abcd);
        }

        public QuadRenderer SetColors(Color a, Color b, Color c, Color d)
        {
            vertices[0].Color = a;
            vertices[1].Color = b;
            vertices[2].Color = c;
            vertices[3].Color = d;

            isDirty = true;

            return this;
        }

        public void Draw(Asset<Effect> effect)
        {
            Draw(effect.Value);
        }

        public void Draw(Effect effect)
        {
            if (isDirty)
            {
                renderer.SetVertices(vertices);

                isDirty = false;
            }

            renderer.Draw(effect, 4, 2);
        }

        public void Dispose()
        {
            renderer?.Dispose();
        }
    }
}
