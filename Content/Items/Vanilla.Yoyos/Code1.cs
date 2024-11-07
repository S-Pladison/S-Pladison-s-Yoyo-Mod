using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class Code1Assets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Code1/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class Code1Item : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Code1;
    }

    public sealed class Code1Projectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Code1;
    }
}