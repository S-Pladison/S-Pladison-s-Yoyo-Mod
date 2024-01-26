using System.Collections.Generic;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public class PlayerEffectFlags : ModPlayer
    {
        private readonly Dictionary<int, bool> flags;

        public PlayerEffectFlags()
        {
            flags = new Dictionary<int, bool>();
        }

        public override void ResetEffects()
        {
            for (int i = 0; i < flags.Count; i++) flags[i] = false;
        }

        public void SetFlag<T>(bool flag = true) where T : ModItem
        {
            flags[ModContent.ItemType<T>()] = flag;
        }

        public bool GetFlag<T>() where T : ModItem
        {
            var type = ModContent.ItemType<T>();
            return flags.ContainsKey(type) && flags[type];
        }
    }
}