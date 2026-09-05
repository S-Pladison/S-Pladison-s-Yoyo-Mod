using Microsoft.Xna.Framework.Graphics;

namespace SPYoyoMod.Utils
{
    public static class SamplerStateUtils
    {
        /// <summary>
        /// Устанавливает значение первого семплера.
        /// </summary>
        public static void Set(this SamplerStateCollection samplers, SamplerState sampler)
            => samplers[0] = sampler;

        /// <summary>
        /// Устанавливает значение первого семплера и возвращает исходное значение через <paramref name="orig"/>.
        /// </summary>
        public static void Set(this SamplerStateCollection samplers, out SamplerState orig, SamplerState sampler)
        {
            orig = samplers[0];
            samplers[0] = sampler;
        }
    }
}
