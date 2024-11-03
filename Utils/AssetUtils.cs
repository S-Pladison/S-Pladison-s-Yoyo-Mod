using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;

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

        public static void Apply(this Asset<Effect> effect, string passName = null)
        {
            if (!effect.IsLoaded)
                return;

            if (passName is not null)
            {
                effect.Value.CurrentTechnique.Passes[passName].Apply();
                return;
            }

            effect.Value.CurrentTechnique.Passes[0].Apply();
        }
    }
}