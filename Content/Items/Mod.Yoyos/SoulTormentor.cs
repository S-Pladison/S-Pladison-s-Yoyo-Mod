using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    public sealed class SoulTormentorAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Mod.Yoyos/SoulTormentor/SoulTormentor";

        public const string ItemPath = $"{YoyoPath}_Item";
        public const string ProjPath = $"{YoyoPath}_Proj";
    }

    public sealed class SoulTormentorItem : YoyoBaseItem
    {
        public const int CharmedEnemyCountMax = 3;

        public override string Texture => SoulTormentorAssets.ItemPath;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CharmedEnemyCountMax);
        public override int GamepadExtraRange => 15;

        public override void SetDefaults()
        {
            base.SetDefaults();

            Item.width = 42;
            Item.height = 26;

            Item.damage = 57;
            Item.knockBack = 3.0f;
            Item.autoReuse = true;

            Item.shoot = ModContent.ProjectileType<SoulTormentorProjectile>();

            Item.rare = ItemRarityID.Lime;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 5, silver: 0, copper: 0);
        }
    }

    public sealed class SoulTormentorProjectile : YoyoBaseProjectile
    {
        public override string Texture => SoulTormentorAssets.ProjPath;
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;
    }
}
