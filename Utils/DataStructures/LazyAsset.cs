using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils.DataStructures
{
    public readonly struct LazyAsset<T>(Func<Asset<T>> valueFactory) where T : class
    {
        private readonly Lazy<Asset<T>> _asset = new(valueFactory);

        public Asset<T> Asset => _asset.Value;
        public T Value => _asset.Value.Value;
        public bool IsUninitialized => _asset is null;

        public static implicit operator Asset<T>(LazyAsset<T> asset) => asset.Asset;

        public static implicit operator T(LazyAsset<T> asset) => asset.Value;

        public static LazyAsset<T> From(string path, AssetRequestMode requestMode = AssetRequestMode.AsyncLoad)
        {
            return new(() => 
                ModContent.Request<T>(path, requestMode)
            );
        }
    }

    public static class LazyAssetExtensions
    {
        public static LazyAsset<Effect> Prepare(this LazyAsset<Effect> effect, Action<EffectParameterCollection> action)
        {
            effect.Asset.Prepare(action);
            return effect;
        }

        public static void Apply(this LazyAsset<Effect> effect, string passName = null)
            => effect.Asset.Apply(passName);
    }
}