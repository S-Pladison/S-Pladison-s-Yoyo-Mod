using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public struct YoyoStatModifiers
    {
        public StatModifier LifeTime;
        public StatModifier MaxRange;
        //public StatModifier TopSpeed;

        public static readonly YoyoStatModifiers Default = new()
        {
            LifeTime = StatModifier.Default,
            MaxRange = StatModifier.Default,
            //TopSpeed = StatModifier.Default,
        };
    }
}