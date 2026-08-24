using SPYoyoMod.Common.Yoyos;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    public sealed class BlackholeAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Mod.Yoyos/Blackhole/Blackhole";

        public const string ItemPath = $"{YoyoPath}_Item";
        public const string ProjPath = $"{YoyoPath}_Proj";
    }

    public sealed class BlackholeItem : YoyoItem<BlackholeProjectile>
    {
        public override string Texture => BlackholeAssets.ItemPath;
        public override int? GamepadExtraRange => 15;

        public override void SetStaticDefaults()
        {
            ModSets.Items.InventoryScaleMultiplier[Type] = 1.3f;
        }

        public override void SetDefaults(Item item)
        {
            item.width = 42;
            item.height = 26;
            item.damage = 90;
            item.knockBack = 2f;
            item.crit = 6;
            item.rare = ItemRarityID.Yellow;
            item.value = ItemUtils.SellPrice(platinum: 0, gold: 20, silver: 0, copper: 0);
        }
    }

    public sealed class BlackholeProjectile : YoyoProjectile<BlackholeItem>
    {
        public override string Texture => BlackholeAssets.ProjPath;
        public override float? LifeTime => -1f;
        public override float? MaxRange => 300f;
        public override float? TopSpeed => 13f;
    }
}
