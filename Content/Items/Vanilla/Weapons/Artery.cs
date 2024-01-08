using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class ArteryYoyoItem : VanillaYoyoItem
    {
        public ArteryYoyoItem() : base(yoyoType: ItemID.CrimsonYoyo) { }
    }

    public class ArteryYoyoProjectile : VanillaYoyoProjectile
    {
        public ArteryYoyoProjectile() : base(yoyoType: ProjectileID.CrimsonYoyo) { }
    }
}