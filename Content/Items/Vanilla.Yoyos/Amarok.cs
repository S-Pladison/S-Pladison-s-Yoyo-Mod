using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class AmarokAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Amarok/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class AmarokItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Amarok;
    }

    public sealed class AmarokProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Amarok;
    }
}