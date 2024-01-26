using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class CodeOneItem : VanillaYoyoItem
    {
        public CodeOneItem() : base(ItemID.Code1) { }
    }

    public class CodeOneProjectile : VanillaYoyoProjectile
    {
        public CodeOneProjectile() : base(ProjectileID.Code1) { }

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            // Тупо проходит сквозь стены + наносит больший урон если враг за стеной
            // Из эффектов - можно тупо прекращать отрисовывать оригинал и отрисовывать *духа*
        }
    }
}