using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace SPYoyoMod.Utils
{
    public static class AssetUtils
    {
        public static Asset<Effect> Prepare(this Asset<Effect> effect, Action<EffectParameterCollection> action)
        {
            if (!effect.IsLoaded)
                return effect;

            action(effect.Value.Parameters);

            return effect;
        }

        public static void Apply(this Asset<Effect> effect, string passName)
        {
            if (!effect.IsLoaded)
                return;

            effect.Value.CurrentTechnique.Passes[passName].Apply();
        }
    }
}