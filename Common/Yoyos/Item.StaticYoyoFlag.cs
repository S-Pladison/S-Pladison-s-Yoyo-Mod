using SPYoyoMod.Utils;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Yoyos
{
    public sealed class StaticYoyoFlagGlobalItem : GlobalItem
    {
        public override void Load()
        {
            // Делаем так, чтобы йо-йо из других модов действительно считались как йо-йо в ванильном понимании...
            ModEvents.OnPostSetupContent += static () =>
            {
                foreach (var (type, item) in ContentSamples.ItemsByType)
                {
                    if (item.IsYoyo())
                    {
                        ItemID.Sets.Yoyo[type] = true;
                    }
                }
            };
        }
    }
}