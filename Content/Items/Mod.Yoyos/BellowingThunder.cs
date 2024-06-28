using SPYoyoMod.Utils;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    public class BellowingThunderAssets
    {
        public const string Item = $"{_path}BellowingThunder_Item";
        public const string Proj = $"{_path}BellowingThunder_Proj";

        private const string _path = $"{nameof(SPYoyoMod)}/Assets/Items/Mod.Yoyos/BellowingThunder/";
    }

    public sealed class BellowingThunderItem : YoyoBaseItem
    {
        public override string Texture => BellowingThunderAssets.Item;
        public override int GamepadExtraRange => 10;

        public override void SetDefaults()
        {
            base.SetDefaults();

            Item.damage = 27;
            Item.knockBack = 3.5f;
            Item.crit = 6;

            Item.shoot = ModContent.ProjectileType<BellowingThunderProjectile>();

            Item.rare = ItemRarityID.Orange;
            Item.value = ItemUtils.SellPrice(platinum: 0, gold: 4, silver: 0, copper: 0);
        }
    }

    public sealed class BellowingThunderProjectile : YoyoBaseProjectile
    {
        public override string Texture => BellowingThunderAssets.Proj;
        public override float LifeTime => -1f;
        public override float MaxRange => 235f;
        public override float TopSpeed => 14f;
    }
}