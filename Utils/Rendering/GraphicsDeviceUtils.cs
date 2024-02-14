using Microsoft.Xna.Framework.Graphics;

namespace SPYoyoMod.Utils.Rendering
{
    public static class GraphicsDeviceExtensions
    {
        public static void PrepRenderState(this GraphicsDevice device, SpriteBatchSnapshot spriteBatchSnapshot)
        {
            device.BlendState = spriteBatchSnapshot.BlendState;
            device.SamplerStates[0] = spriteBatchSnapshot.SamplerState;
            device.DepthStencilState = spriteBatchSnapshot.DepthStencilState;
            device.RasterizerState = spriteBatchSnapshot.RasterizerState;
        }
    }
}