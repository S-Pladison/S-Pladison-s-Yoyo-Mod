using SPYoyoMod.Utils;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    public sealed class BlackholeAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Mod.Yoyos/Blackhole/Blackhole";

        public const string ItemPath = $"{YoyoPath}_Item";
        public const string ProjPath = $"{YoyoPath}_Proj";
    }

    public sealed class BlackholeItem : YoyoBaseItem
    {
        public override string Texture => BlackholeAssets.ItemPath;
        public override int GamepadExtraRange => 15;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();

            ModSets.Items.InventoryScaleMultiplier[Type] = 1.3f;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();

            Item.width = 42;
            Item.height = 26;

            Item.damage = 90;
            Item.knockBack = 2f;
            Item.crit = 6;

            Item.shoot = ModContent.ProjectileType<BlackholeProjectile>();

            Item.rare = ItemRarityID.Yellow;
            Item.value = ItemUtils.SellPrice(platinum: 0, gold: 20, silver: 0, copper: 0);
        }
    }

    public sealed class BlackholeProjectile : YoyoBaseProjectile
    {
        public override string Texture => BlackholeAssets.ProjPath;
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;
    }
}
