using Microsoft.Xna.Framework.Graphics;

namespace SPYoyoMod.Utils.Rendering
{
    public static class SpriteBatchExtensions
    {
        public static void Begin(this SpriteBatch spriteBatch, SpriteBatchSnapshot spriteBatchSnapshot)
        {
            spriteBatch.Begin
            (
                spriteBatchSnapshot.SortMode, spriteBatchSnapshot.BlendState, spriteBatchSnapshot.SamplerState, spriteBatchSnapshot.DepthStencilState,
                spriteBatchSnapshot.RasterizerState, spriteBatchSnapshot.Effect, spriteBatchSnapshot.Matrix
            );
        }

        public static void End(this SpriteBatch spriteBatch, out SpriteBatchSnapshot spriteBatchSnapshot)
        {
            spriteBatchSnapshot = new SpriteBatchSnapshot(spriteBatch);
            spriteBatch.End();
        }
    }
}