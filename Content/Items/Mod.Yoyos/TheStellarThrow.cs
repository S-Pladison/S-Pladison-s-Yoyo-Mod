using SPYoyoMod.Utils;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class TheStellarThrowAssets : ILoadable
    {
        public const string ItemPath = $"{_yoyoPath}TheStellarThrow_Item";
        public const string ProjPath = $"{_yoyoPath}TheStellarThrow_Proj";

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Mod.Yoyos/TheStellarThrow/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class TheStellarThrowItem : YoyoBaseItem
    {
        public override string Texture => TheStellarThrowAssets.ItemPath;
        public override int GamepadExtraRange => 10;

        public override void SetDefaults()
        {
            base.SetDefaults();

            Item.damage = 18;
            Item.knockBack = 3f;

            Item.shoot = ModContent.ProjectileType<TheStellarThrowProjectile>();

            Item.rare = ItemRarityID.Green;
            Item.value = ItemUtils.SellPrice(platinum: 0, gold: 1, silver: 0, copper: 0);
        }
    }

    public sealed class TheStellarThrowProjectile : YoyoBaseProjectile
    {
        public override string Texture => TheStellarThrowAssets.ProjPath;
        public override float LifeTime => -1f;
        public override float MaxRange => 235f;
        public override float TopSpeed => 14f;
    }
}
