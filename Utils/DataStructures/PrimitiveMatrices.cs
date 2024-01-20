using Microsoft.Xna.Framework;

namespace SPYoyoMod.Utils.DataStructures
{
    public readonly struct PrimitiveMatrices
    {
        public readonly Matrix Transform;
        public readonly Matrix TransformWithScreenOffset;

        public PrimitiveMatrices(Matrix transform, Matrix transformWithScreenOffset)
        {
            Transform = transform;
            TransformWithScreenOffset = transformWithScreenOffset;
        }
    }
}