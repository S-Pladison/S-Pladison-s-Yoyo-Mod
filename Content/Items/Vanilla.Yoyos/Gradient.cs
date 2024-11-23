using Microsoft.Xna.Framework;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class GradientAssets : ILoadable
    {
        public const string InvisiblePath = $"{_assetPath}Invisible";

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Gradient/";

        void ILoadable.Unload()
        {
            // ...
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class GradientItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Gradient;
    }

    public sealed class GradientProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Gradient;

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // TODO: Понижать шанс нанесения метки, если рядом уже есть враг с меткой

            var markType = ModContent.ProjectileType<GradientMarkProjectile>();

            if (proj.GetOwner().ownedProjectileCounts[markType] == 0)
            {
                Projectile.NewProjectile(proj.GetSource_OnHit(proj), target.Center, Vector2.Zero, ModContent.ProjectileType<GradientMarkProjectile>(), proj.damage, proj.knockBack, proj.owner, target.whoAmI);
                return;
            }

            foreach (var otherProj in Main.ActiveProjectiles)
            {
                if (otherProj.type != markType || otherProj.owner != proj.owner)
                    continue;

                if ((otherProj.As<GradientMarkProjectile>().Target?.whoAmI ?? -1) == target.whoAmI)
                    return;
            }

            Projectile.NewProjectile(proj.GetSource_OnHit(proj), target.Center, Vector2.Zero, ModContent.ProjectileType<GradientMarkProjectile>(), proj.damage, proj.knockBack, proj.owner, target.whoAmI);
        }
    }

    public sealed class GradientMarkProjectile : ModProjectile
    {
        public static readonly int InitTimeLeft = ModUtils.SecondsToTicks(3f);

        public override string Texture { get => GradientAssets.InvisiblePath; }
        public NPC Target { get => (int)Projectile.ai[0] >= 0 ? Main.npc[(int)Projectile.ai[0]] : null; }

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;

            Projectile.timeLeft = InitTimeLeft;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            if (Target is null || !Target.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Target.Center;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }
    }
}