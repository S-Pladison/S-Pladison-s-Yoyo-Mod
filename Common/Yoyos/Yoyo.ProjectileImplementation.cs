using SPYoyoMod.Core;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Yoyos
{
    public abstract partial class YoyoProjectile
    {
        [LoadBefore(typeof(YoyoProjectile))]
        private sealed class OverrideGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
            {
                if (!lateInstantiation)
                    return false;

                return TryGet(proj.type, out var definition) && definition.IsOverride;
            }

            public override void SetStaticDefaults()
            {
                foreach (var definition in ModContent.GetContent<YoyoProjectile>())
                {
                    if (!definition.IsOverride)
                        continue;

                    if (definition.LifeTime.HasValue)
                        ProjectileID.Sets.YoyosLifeTimeMultiplier[definition.Type] = definition.LifeTime.Value;

                    if (definition.MaxRange.HasValue)
                        ProjectileID.Sets.YoyosMaximumRange[definition.Type] = definition.MaxRange.Value;

                    if (definition.TopSpeed.HasValue)
                        ProjectileID.Sets.YoyosTopSpeed[definition.Type] = definition.TopSpeed.Value;
                }
            }
        }

        [Autoload(false)]
        private sealed class ModProjectileStub<T> : ModProjectile where T : YoyoProjectile
        {
            private static T Definition => Get<T>();

            public override string Name => typeof(T).Name;
            public override string Texture => Definition.Texture;

            public override void SetStaticDefaults()
            {
                if (Definition.LifeTime.HasValue)
                    ProjectileID.Sets.YoyosLifeTimeMultiplier[Type] = Definition.LifeTime.Value;

                if (Definition.MaxRange.HasValue)
                    ProjectileID.Sets.YoyosMaximumRange[Type] = Definition.MaxRange.Value;

                if (Definition.TopSpeed.HasValue)
                    ProjectileID.Sets.YoyosTopSpeed[Type] = Definition.TopSpeed.Value;
            }

            public override void SetDefaults()
            {
                Projectile.DamageType = DamageClass.MeleeNoSpeed;
                Projectile.width = 16;
                Projectile.height = 16;
                Projectile.aiStyle = ProjAIStyleID.Yoyo;
                Projectile.friendly = true;
                Projectile.penetrate = -1;
            }
        }
    }
}
