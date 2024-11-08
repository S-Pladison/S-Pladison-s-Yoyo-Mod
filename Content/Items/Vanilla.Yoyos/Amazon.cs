using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class AmazonAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Amazon/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class AmazonItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.JungleYoyo;
    }

    public sealed class AmazonProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.JungleYoyo;
    }
}