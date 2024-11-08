using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class GradientAssets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Gradient/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class GradientItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Gradient;
    }

    public sealed class GradientProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Gradient;
    }
}