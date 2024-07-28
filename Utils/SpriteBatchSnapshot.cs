using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SPYoyoMod.Utils
{
    public struct SpriteBatchSnapshot
    {
        private static readonly Func<SpriteBatch, SpriteSortMode> _sortModeFieldAccessor;
        private static readonly Func<SpriteBatch, BlendState> _blendStateFieldAccessor;
        private static readonly Func<SpriteBatch, SamplerState> _samplerStateFieldAccessor;
        private static readonly Func<SpriteBatch, DepthStencilState> _depthStencilStateFieldAccessor;
        private static readonly Func<SpriteBatch, RasterizerState> _rasterizerStateFieldAccessor;
        private static readonly Func<SpriteBatch, Effect> _effectFieldAccessor;
        private static readonly Func<SpriteBatch, Matrix> _matrixFieldAccessor;

        public SpriteSortMode SortMode;
        public BlendState BlendState;
        public SamplerState SamplerState;
        public DepthStencilState DepthStencilState;
        public RasterizerState RasterizerState;
        public Effect Effect;
        public Matrix Matrix;

        static SpriteBatchSnapshot()
        {
            _sortModeFieldAccessor = TypeUtils.GetFieldAccessor<SpriteBatch, SpriteSortMode>("sortMode");
            _blendStateFieldAccessor = TypeUtils.GetFieldAccessor<SpriteBatch, BlendState>("blendState");
            _samplerStateFieldAccessor = TypeUtils.GetFieldAccessor<SpriteBatch, SamplerState>("samplerState");
            _depthStencilStateFieldAccessor = TypeUtils.GetFieldAccessor<SpriteBatch, DepthStencilState>("depthStencilState");
            _rasterizerStateFieldAccessor = TypeUtils.GetFieldAccessor<SpriteBatch, RasterizerState>("rasterizerState");
            _effectFieldAccessor = TypeUtils.GetFieldAccessor<SpriteBatch, Effect>("customEffect");
            _matrixFieldAccessor = TypeUtils.GetFieldAccessor<SpriteBatch, Matrix>("transformMatrix");
        }

        public SpriteBatchSnapshot(SpriteBatch spriteBatch)
        {
            if (spriteBatch is null)
                throw new ArgumentNullException(nameof(spriteBatch));

            SortMode = _sortModeFieldAccessor(spriteBatch);
            BlendState = _blendStateFieldAccessor(spriteBatch);
            SamplerState = _samplerStateFieldAccessor(spriteBatch);
            DepthStencilState = _depthStencilStateFieldAccessor(spriteBatch);
            RasterizerState = _rasterizerStateFieldAccessor(spriteBatch);
            Effect = _effectFieldAccessor(spriteBatch);
            Matrix = _matrixFieldAccessor(spriteBatch);
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