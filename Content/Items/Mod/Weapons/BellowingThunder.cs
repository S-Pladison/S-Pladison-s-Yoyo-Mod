using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    // Что-то типа грани ночи (по рецепту и этапу получения)
    // Рецепт всеж не должен быть параллельным указанному мечу
    // Например, Доблесть (Valor) точно не должен учавствовать в рецепте... На него другие планы
    // Так-же не Каскад (Cascade)... Он дропается фиг знает как, но это не основная причина...
    // Сам йо-йо подобен данному эффекту: https://www.artstation.com/artwork/3qdG4E
    // Ударяем молнией, создаем копию йо-йо/сферку, который в свою очередь делится на 2 другие мелки сферки
    // Кароче, все по ссылке видно...

    public class BellowingThunderItem : YoyoItem
    {
        public override string Texture => ModAssets.ItemsPath + "BellowingThunder";
        public override int GamepadExtraRange => 15;

        public override void YoyoSetDefaults()
        {
            Item.damage = 43;
            Item.knockBack = 2.5f;

            Item.shoot = ModContent.ProjectileType<BellowingThunderProjectile>();

            Item.rare = ItemRarityID.Orange;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.CorruptYoyo)
                .AddIngredient<FreezingPointItem>()
                .AddIngredient(ItemID.JungleYoyo)
                .AddTile(TileID.DemonAltar)
                .Register();

            CreateRecipe()
                .AddIngredient(ItemID.CrimsonYoyo)
                .AddIngredient<FreezingPointItem>()
                .AddIngredient(ItemID.JungleYoyo)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }

    public class BellowingThunderProjectile : YoyoProjectile
    {
        public override string Texture => ModAssets.ProjectilesPath + "BellowingThunder";
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;
    }
}