using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class ArteryAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Artery/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class ArteryItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.CrimsonYoyo;
    }

    public sealed class ArteryProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.CrimsonYoyo;
    }
}