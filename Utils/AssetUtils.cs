using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;

namespace SPYoyoMod.Utils
{
    public static class AssetExtensions
    {
        public static Asset<Effect> Prepare(this Asset<Effect> effect, Action<EffectParameterCollection> action)
        {
            if (!effect.IsLoaded)
                throw new Exception($"{effect.Name} is not loaded...");

            action(effect.Value.Parameters);

            return effect;
        }
    }
}