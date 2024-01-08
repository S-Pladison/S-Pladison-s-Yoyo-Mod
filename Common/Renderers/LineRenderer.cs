using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace SPYoyoMod.Common.Renderers
{
    public class LineRenderer
    {
        private PrimitiveRenderer renderer;

        public LineRenderer()
        {
            renderer = null;
        }

        public void Draw(Asset<Effect> effect)
        {
            Draw(effect.Value);
        }

        public void Draw(Effect effect)
        {
            renderer.Draw(effect);
        }
    }
}