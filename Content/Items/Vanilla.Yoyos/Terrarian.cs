using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class TerrarianAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Terrarian/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class TerrarianItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Terrarian;
    }

    public sealed class TerrarianProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Terrarian;
    }
}