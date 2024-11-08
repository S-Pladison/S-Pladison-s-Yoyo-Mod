using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class ChikAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Chik/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class ChikItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Chik;
    }

    public sealed class ChikProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Chik;
    }
}