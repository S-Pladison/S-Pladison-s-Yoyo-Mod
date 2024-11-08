using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class YeletsAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Yelets/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class YeletsItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Yelets;
    }

    public sealed class YeletsProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Yelets;
    }
}