using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class RallyAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Rally/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class RallyItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Rally;
    }

    public sealed class RallyProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Rally;
    }
}