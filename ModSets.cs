using SPYoyoMod.Core;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod
{
    [LoadBefore]
    public sealed class ModSets : ILoadable
    {
        public sealed class Items
        {
            /// <summary>
            /// Отвечает за модификацию масштаба отрисовки предмета в слоте инвентаря.
            /// </summary>
            public static float?[] InventoryScaleMultiplier { get; internal set; } = Items.InventoryScaleMultiplier = ItemID.Sets.Factory.CreateCustomSet<float?>(null);
        }

        void ILoadable.Load(Mod mod)
        {
            // ...
        }

        void ILoadable.Unload()
        {
            Items.InventoryScaleMultiplier = null;
        }
    }
}