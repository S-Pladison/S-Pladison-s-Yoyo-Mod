using SPYoyoMod.Utils.Extensions;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class ValorItem : VanillaYoyoItem
    {
        public ValorItem() : base(ItemID.Valor) { }
    }

    public class ValorProjectile : VanillaYoyoProjectile
    {
        public override bool InstancePerEntity { get => true; }
        public bool IsMainYoyo { get; private set; }

        public ValorProjectile() : base(ProjectileID.Valor) { }

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            IsMainYoyo = GetMainYoyoFlag(proj);
        }

        public override void AI(Projectile proj)
        {
            if (!IsMainYoyo) return;

            Dust.NewDustPerfect(proj.Center, DustID.Torch);
        }

        private bool GetMainYoyoFlag(Projectile proj)
        {
            var owner = Main.player[proj.owner];

            if (owner.OwnedProjectileCounts(proj.type) > 0)
                return false;

            // Fact that owned proj count was 0 does not guarantee that it is main yoyo
            // (In case of spawning 2+ yoyos at once)
            // Therefore, let's check other projs

            for (int i = 0; i < proj.whoAmI; i++)
            {
                ref var otherProjectile = ref Main.projectile[i];

                if (otherProjectile.active
                    && otherProjectile.owner == proj.owner
                    && otherProjectile.type == proj.type
                    && otherProjectile.TryGetGlobalProjectile(out ValorProjectile otherGlobalProj)
                    && otherGlobalProj.IsMainYoyo)
                    return false;
            }

            return true;
        }
    }
}