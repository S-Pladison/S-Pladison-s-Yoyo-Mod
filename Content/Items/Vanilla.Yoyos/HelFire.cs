using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class HelFireAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/HelFire/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class HelFireItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.HelFire;
    }

    public sealed class HelFireProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.HelFire;
    }
}