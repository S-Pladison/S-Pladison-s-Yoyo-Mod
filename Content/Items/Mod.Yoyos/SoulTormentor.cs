using SPYoyoMod.Common.Yoyos;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    public sealed class SoulTormentorAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Mod.Yoyos/SoulTormentor/SoulTormentor";

        public const string ItemPath = $"{YoyoPath}_Item";
        public const string ProjPath = $"{YoyoPath}_Proj";
    }

    public sealed class SoulTormentorItem : YoyoItem<SoulTormentorProjectile>
    {
        public const int CharmedEnemyCountMax = 3;

        public override string Texture => SoulTormentorAssets.ItemPath;
        public override int? GamepadExtraRange => 15;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CharmedEnemyCountMax);

        public override void SetDefaults(Item item)
        {
            item.width = 42;
            item.height = 26;
            item.damage = 57;
            item.knockBack = 3.0f;
            item.autoReuse = true;
            item.rare = ItemRarityID.Lime;
            item.value = ItemUtils.SellPrice(platinum: 0, gold: 5, silver: 0, copper: 0);
        }
    }

    public sealed class SoulTormentorProjectile : YoyoProjectile<SoulTormentorItem>
    {
        public override string Texture => SoulTormentorAssets.ProjPath;
        public override float? LifeTime => -1f;
        public override float? MaxRange => 300f;
        public override float? TopSpeed => 13f;
    }
}
