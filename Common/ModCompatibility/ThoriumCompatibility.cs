using Terraria.ModLoader;

namespace SPYoyoMod.Common.ModCompatibility
{
    public sealed class ThoriumCompatibility : ModCompatibility
    {
        public override string ModName { get => "ThoriumMod"; }

        public void AddMartianItem(int type)
        {
            if (!IsModLoaded) return;

            if (Mod.Call("AddMartianItemID", type) is not bool value || !value)
                ModContent.GetInstance<SPYoyoMod>().Logger.Error($"Error:[Failed to call 'AddMartianItemID'] Mod:[{ModName}] Parameters:[{type}]");
        }
    }
}