using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class HiveFiveAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/HiveFive/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class HiveFiveItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.HiveFive;
    }

    public sealed class HiveFiveProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.HiveFive;
    }
}