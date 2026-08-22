using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Core;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    public static class GameMatrices
    {
        public static Matrix Zoom { get => Main.GameViewMatrix.ZoomMatrix; }
        public static Matrix Effect { get; private set; }
        public static Matrix World { get; private set; }
        public static Matrix Projection { get; private set; }
        public static Matrix Transform { get => Main.GameViewMatrix.TransformationMatrix; }

        [LoadBefore] //< Для того, чтобы подписка на ModEvents.OnPostUpdateCameraPosition вызывалась раньше остальных...
        private sealed class GameMatricesHandler : ILoadable
        {
            private static void RecalculateMatrices()
            {
                var viewport = Main.graphics.GraphicsDevice.Viewport;
                var spriteEffect = (!Main.gameMenu && Main.LocalPlayer.gravDir != 1f) ? SpriteEffects.FlipVertically : SpriteEffects.None;

                Effect = Matrix.Identity;

                if (spriteEffect.HasFlag(SpriteEffects.FlipHorizontally))
                    Effect *= Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateTranslation(viewport.Width, 0f, 0f);

                if (spriteEffect.HasFlag(SpriteEffects.FlipVertically))
                    Effect *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, viewport.Height, 0f);

                World = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0);

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