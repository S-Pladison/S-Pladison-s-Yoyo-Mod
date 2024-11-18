using SPYoyoMod.Utils;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class TitaniumYoyoAssets : ILoadable
    {
        public const string ItemPath = $"{_yoyoPath}TitaniumYoyo_Item";
        public const string ProjPath = $"{_yoyoPath}TitaniumYoyo_Proj";

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Mod.Yoyos/TitaniumYoyo/";

        void ILoadable.Unload()
        {
            
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class TitaniumYoyoItem : YoyoBaseItem
    {
        public override string Texture => TitaniumYoyoAssets.ItemPath;
        public override int GamepadExtraRange => 15;

        public override void SetDefaults()
        {
            base.SetDefaults();

            Item.damage = 42;
            Item.knockBack = 2.5f;

            Item.shoot = ModContent.ProjectileType<TitaniumYoyoProjectile>();

            Item.rare = ItemRarityID.LightRed;
            Item.value = ItemUtils.SellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    public sealed class TitaniumYoyoProjectile : YoyoBaseProjectile
    {
        public override string Texture => TitaniumYoyoAssets.ProjPath;
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;
    }
}