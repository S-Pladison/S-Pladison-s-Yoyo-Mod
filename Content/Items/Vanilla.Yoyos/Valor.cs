using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class ValorAssets : ILoadable
    {
        // [ Общее ]
        private const string _path = $"{nameof(SPYoyoMod)}/Assets/Items/Vanilla.Yoyos/Valor/";

        void ILoadable.Unload() { }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class ValorItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Valor;
    }

    public sealed class ValorProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Valor;
    }
}