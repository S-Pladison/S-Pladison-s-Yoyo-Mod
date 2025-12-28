using System.Collections.Generic;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Yoyos
{
    public sealed class StaticYoyoFlagSystem : ModSystem
    {
        private readonly HashSet<int> _yoyosWithoutYoyoFlag = [];

        public override void Load()
        {
            // Делаем так, чтобы йо-йо из других модов действительно считались как йо-йо в ванильном понимании...
            ModEvents.OnPostSetupContent += static () =>
            {
                var instance = ModContent.GetInstance<StaticYoyoFlagSystem>();

                foreach (var (type, item) in ContentSamples.ItemsByType)
                {
                    if (!item.IsYoyo() || ItemID.Sets.Yoyo[type])
                        continue;

                    instance._yoyosWithoutYoyoFlag.Add(type);
                }
            };
        }

        public override void Unload()
        {
            foreach (var type in _yoyosWithoutYoyoFlag)
            {
                ItemID.Sets.Yoyo[type] = false;
            }

            _yoyosWithoutYoyoFlag.Clear();
        }
    }
}