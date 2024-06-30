using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SPYoyoMod.Utils
{
    public struct SpriteBatchSnapshot
    {
        private static readonly Func<SpriteBatch, SpriteSortMode> sortModeFieldAccessor;
        private static readonly Func<SpriteBatch, BlendState> blendStateFieldAccessor;
        private static readonly Func<SpriteBatch, SamplerState> samplerStateFieldAccessor;
        private static readonly Func<SpriteBatch, DepthStencilState> depthStencilStateFieldAccessor;
        private static readonly Func<SpriteBatch, RasterizerState> rasterizerStateFieldAccessor;
        private static readonly Func<SpriteBatch, Effect> effectFieldAccessor;
        private static readonly Func<SpriteBatch, Matrix> matrixFieldAccessor;

        public SpriteSortMode SortMode;
        public BlendState BlendState;
        public SamplerState SamplerState;
        public DepthStencilState DepthStencilState;
        public RasterizerState RasterizerState;
        public Effect Effect;
        public Matrix Matrix;

        static SpriteBatchSnapshot()
        {
            sortModeFieldAccessor = ModUtils.GetFieldAccessor<SpriteBatch, SpriteSortMode>("sortMode");
            blendStateFieldAccessor = ModUtils.GetFieldAccessor<SpriteBatch, BlendState>("blendState");
            samplerStateFieldAccessor = ModUtils.GetFieldAccessor<SpriteBatch, SamplerState>("samplerState");
            depthStencilStateFieldAccessor = ModUtils.GetFieldAccessor<SpriteBatch, DepthStencilState>("depthStencilState");
            rasterizerStateFieldAccessor = ModUtils.GetFieldAccessor<SpriteBatch, RasterizerState>("rasterizerState");
            effectFieldAccessor = ModUtils.GetFieldAccessor<SpriteBatch, Effect>("customEffect");
            matrixFieldAccessor = ModUtils.GetFieldAccessor<SpriteBatch, Matrix>("transformMatrix");
        }

        public SpriteBatchSnapshot(SpriteBatch spriteBatch)
        {
            if (spriteBatch is null)
                throw new ArgumentNullException(nameof(spriteBatch));

            SortMode = sortModeFieldAccessor(spriteBatch);
            BlendState = blendStateFieldAccessor(spriteBatch);
            SamplerState = samplerStateFieldAccessor(spriteBatch);
            DepthStencilState = depthStencilStateFieldAccessor(spriteBatch);
            RasterizerState = rasterizerStateFieldAccessor(spriteBatch);
            Effect = effectFieldAccessor(spriteBatch);
            Matrix = matrixFieldAccessor(spriteBatch);
        }
    }

    public static class SpriteBatchSnapshotExtensions
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