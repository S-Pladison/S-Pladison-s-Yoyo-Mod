using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Dusts
{
    public abstract class ModDust<T> : ModDust where T : class
    {
        public sealed override bool Update(Dust dust)
        {
            if (dust.customData is not T customData)
            {
                dust.active = false;
                return false;
            }

            return Update(dust, customData);
        }

        public virtual bool Update(Dust dust, T customData)
        {
            return true;
        }

        public sealed override bool PreDraw(Dust dust)
        {
            if (dust.customData is not T customData)
            {
                dust.active = false;
                return false;
            }

            return PreDraw(dust, customData);
        }

        public virtual bool PreDraw(Dust dust, T customData)
        {
            return true;
        }
    }
}