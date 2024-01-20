using Microsoft.Xna.Framework;

namespace SPYoyoMod.Utils.DataStructures
{
    public readonly struct PrimitiveMatrices
    {
        public readonly Matrix Transform;
        public readonly Matrix TransformWithoutScreenOffset;

        public PrimitiveMatrices(Matrix transform, Matrix transformWithoutScreenOffset)
        {
            Transform = transform;
            TransformWithoutScreenOffset = transformWithoutScreenOffset;
        }
    }
}