using SPYoyoMod.Utils;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Core.ModSupport
{
    [ModInternalName("ThoriumMod")]
    public sealed class ThoriumSupport : ModSupportSystem<ThoriumSupport>
    {
        private ThoriumSupport() { }

        public override void PostSetupContent()
        {
            // Делаем так, чтобы их йо-йо действительно были йо-йо в ванильном понимании...
            foreach (var modItem in Data.Instance.GetContent<ModItem>())
            {
                var itemType = modItem.Type;

                if (ContentSamples.ItemsByType[itemType].IsYoyo())
                    ItemID.Sets.Yoyo[itemType] = true;
            }
        }
    }
}