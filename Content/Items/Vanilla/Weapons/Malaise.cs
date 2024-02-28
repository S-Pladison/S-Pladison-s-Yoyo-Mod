using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class MalaiseItem : VanillaYoyoItem
    {
        public override int YoyoType => ItemID.CorruptYoyo;
    }

    public class MalaiseProjectile : VanillaYoyoProjectile
    {
        public override int YoyoType => ProjectileID.CorruptYoyo;
    }
}