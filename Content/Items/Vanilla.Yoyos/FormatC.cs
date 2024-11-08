using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class FormatCAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/FormatC/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class FormatCItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.FormatC;
    }

    public sealed class FormatCProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.FormatC;
    }
}