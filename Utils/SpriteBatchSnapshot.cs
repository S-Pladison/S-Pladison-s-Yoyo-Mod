using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;

namespace SPYoyoMod.Utils
{
    public struct SpriteBatchSnapshot
    {
        public SpriteSortMode SortMode;
        public BlendState BlendState;
        public SamplerState SamplerState;
        public DepthStencilState DepthStencilState;
        public RasterizerState RasterizerState;
        public Effect Effect;
        public Matrix Matrix;

        public SpriteBatchSnapshot(SpriteBatch spriteBatch)
        {
            ArgumentNullException.ThrowIfNull(spriteBatch, nameof(spriteBatch));

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "sortMode")]
            extern static ref SpriteSortMode GetSetSortMode(SpriteBatch sbInstance);

            SortMode = GetSetSortMode(spriteBatch);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "blendState")]
            extern static ref BlendState GetSetBlendState(SpriteBatch sbInstance);

            BlendState = GetSetBlendState(spriteBatch);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "samplerState")]
            extern static ref SamplerState GetSetSamplerState(SpriteBatch sbInstance);

            SamplerState = GetSetSamplerState(spriteBatch);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "depthStencilState")]
            extern static ref DepthStencilState GetSetDepthStencilState(SpriteBatch sbInstance);

            DepthStencilState = GetSetDepthStencilState(spriteBatch);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "rasterizerState")]
            extern static ref RasterizerState GetSetRasterizerState(SpriteBatch sbInstance);

            RasterizerState = GetSetRasterizerState(spriteBatch);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "customEffect")]
            extern static ref Effect GetSetEffect(SpriteBatch sbInstance);

            Effect = GetSetEffect(spriteBatch);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "transformMatrix")]
            extern static ref Matrix GetSetMatrix(SpriteBatch sbInstance);

            Matrix = GetSetMatrix(spriteBatch);
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