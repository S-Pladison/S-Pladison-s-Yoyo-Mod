using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod
{
    public static class ModSets
    {
        public class Items : ILoadable
        {
            public static float?[] InventoryDrawScaleMultiplier { get; private set; }
                = ItemID.Sets.Factory.CreateCustomSet<float?>(null);

            void ILoadable.Load(Terraria.ModLoader.Mod mod) { }

            void ILoadable.Unload()
            {
                InventoryDrawScaleMultiplier = null;
            }
        }
    }
}