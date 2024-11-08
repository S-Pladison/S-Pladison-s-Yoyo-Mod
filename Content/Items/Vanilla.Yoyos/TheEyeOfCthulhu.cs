using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class TheEyeOfCthulhuAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/TheEyeOfCthulhu/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class TheEyeOfCthulhuItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.TheEyeOfCthulhu;
    }

    public sealed class TheEyeOfCthulhuProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.TheEyeOfCthulhu;
    }
}