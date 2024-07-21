using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    public static class GameMatrices
    {
        public static Matrix Zoom { get => Main.GameViewMatrix.ZoomMatrix; }
        public static Matrix Effect { get => GameMatricesHandler.Effect; }
        public static Matrix Transform { get => Main.GameViewMatrix.TransformationMatrix; }
        public static Matrix Projection { get => GameMatricesHandler.Projection; }

        private sealed class GameMatricesHandler : ILoadable
        {
            public static Matrix Effect;
            public static Matrix Projection;

            private static void RecalculateMatrices()
            {
                var viewport = Main.graphics.GraphicsDevice.Viewport;
                var spriteEffect = (!Main.gameMenu && Main.LocalPlayer.gravDir != 1f) ? SpriteEffects.FlipVertically : SpriteEffects.None;

                Effect = Matrix.Identity;

		        if (spriteEffect.HasFlag(SpriteEffects.FlipHorizontally))
			        Effect *= Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateTranslation(viewport.Width, 0f, 0f);

		        if (spriteEffect.HasFlag(SpriteEffects.FlipVertically))
			        Effect *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, viewport.Height, 0f);

                Projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0f, -1f, 1f);
            }

            public void Load(Mod mod)
            {
                ModEvents.OnPostUpdateCameraPosition += RecalculateMatrices;
            }

            public void Unload()
            {
                ModEvents.OnPostUpdateCameraPosition -= RecalculateMatrices;
            }
        }
    }
}