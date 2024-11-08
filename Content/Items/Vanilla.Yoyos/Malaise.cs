using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class MalaiseAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Malaise/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class MalaiseItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.CorruptYoyo;
    }

    public sealed class MalaiseProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.CorruptYoyo;
    }
}