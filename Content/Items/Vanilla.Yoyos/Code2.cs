using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class Code2Assets : ILoadable
    {
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Code2/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class Code2Item : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Code2;
    }

    public sealed class Code2Projectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Code2;
    }
}