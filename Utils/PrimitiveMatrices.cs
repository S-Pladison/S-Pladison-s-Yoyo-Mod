using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    public readonly struct PrimitiveMatrices
    {
        public static PrimitiveMatrices DefaultPrimitiveMatrices => ModContent.GetInstance<PrimitiveMatricesLoader>().DefaultPrimitiveMatrices;
        public static PrimitiveMatrices PixelatedPrimitiveMatrices => ModContent.GetInstance<PrimitiveMatricesLoader>().PixelatedPrimitiveMatrices;

        public readonly Matrix Transform;
        public readonly Matrix TransformWithScreenOffset;

        public PrimitiveMatrices(Matrix transform, Matrix transformWithScreenOffset)
        {
            Transform = transform;
            TransformWithScreenOffset = transformWithScreenOffset;
        }

        private class PrimitiveMatricesLoader : ILoadable
        {
            public PrimitiveMatrices DefaultPrimitiveMatrices { get; private set; }
            public PrimitiveMatrices PixelatedPrimitiveMatrices { get; private set; }

            private Matrix projectionMatrix;

            void ILoadable.Load(Mod mod)
            {
                projectionMatrix = new Matrix(0f, 0f, 0f, 0f,
                                              0f, 0f, 0f, 0f,
                                              0f, 0f, 1f, 0f,
                                             -1f, 1f, 0f, 1f);

                On_Main.DoDraw_UpdateCameraPosition += (orig) =>
                {
                    orig();
                    UpdateMatrices();
                };
            }

            void ILoadable.Unload() { }

            private void UpdateMatrices()
            {
                var viewport = Main.graphics.GraphicsDevice.Viewport;
                var screenOffsetMatrix = Matrix.CreateTranslation(new Vector3(-Main.screenPosition, 0));

                projectionMatrix.M11 = 2f / viewport.Width;
                projectionMatrix.M22 = -2f / viewport.Height;
                projectionMatrix.M41 = -1f - 0.5f * projectionMatrix.M11;
                projectionMatrix.M42 = 1f - 0.5f * projectionMatrix.M22;

                var transformMatrix = Main.GameViewMatrix.TransformationMatrix;
                var transformWithScreenOffsetMatrix = transformMatrix;
                var pixelatedTransformMatrix = Main.GameViewMatrix.EffectMatrix;
                var pixelatedTransformWithScreenOffsetMatrix = pixelatedTransformMatrix;

                Matrix.Multiply(ref screenOffsetMatrix, ref transformWithScreenOffsetMatrix, out transformWithScreenOffsetMatrix);
                Matrix.Multiply(ref screenOffsetMatrix, ref pixelatedTransformWithScreenOffsetMatrix, out pixelatedTransformWithScreenOffsetMatrix);

                Matrix.Multiply(ref transformMatrix, ref projectionMatrix, out transformMatrix);
                Matrix.Multiply(ref transformWithScreenOffsetMatrix, ref projectionMatrix, out transformWithScreenOffsetMatrix);

                DefaultPrimitiveMatrices = new PrimitiveMatrices(transformMatrix, transformWithScreenOffsetMatrix);

                Matrix.Multiply(ref pixelatedTransformMatrix, ref projectionMatrix, out pixelatedTransformMatrix);
                Matrix.Multiply(ref pixelatedTransformWithScreenOffsetMatrix, ref projectionMatrix, out pixelatedTransformWithScreenOffsetMatrix);

                PixelatedPrimitiveMatrices = new PrimitiveMatrices(pixelatedTransformMatrix, pixelatedTransformWithScreenOffsetMatrix);
            }
        }
    }
}