using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;

namespace SPYoyoMod.Utils
{
    public static class AssetUtils
    {
        /// <summary>
        /// Выполняет подготовительные действия для заданного эффекта, если он был загружен.
        /// </summary>
        /// <param name="effect">Эффект.</param>
        /// <param name="action">Действие, которое будет применено к параметрам эффекта.</param>
        /// <returns>Исходный <see cref="Asset{Effect}"/> для цепочки вызовов.</returns>
        public static Asset<Effect> Prepare(this Asset<Effect> effect, Action<EffectParameterCollection> action)
        {
            if (!effect.IsLoaded)
                return effect;

            action(effect.Value.Parameters);

            return effect;
        }

        /// <summary>
        /// Применяет указанный проход эффекта, если он загружен.
        /// </summary>
        /// <param name="effect">Эффект.</param>
        /// <param name="passName">Имя прохода техники для применения. Если не указано, применяется первый доступный проход.</param>
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