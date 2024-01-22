using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod
{
    public class ModSets : ILoadable
    {
        public static class Items
        {
            public static ref float?[] InventoryDrawScaleMultiplier { get => ref innerItemInventoryDrawScaleMultiplier; }
        }

        private static float?[] innerItemInventoryDrawScaleMultiplier = ItemID.Sets.Factory.CreateCustomSet<float?>(null);

        void ILoadable.Unload()
        {
            innerItemInventoryDrawScaleMultiplier = null;
        }

        void ILoadable.Load(Mod mod)
        {
            // ...
        }
    }
}