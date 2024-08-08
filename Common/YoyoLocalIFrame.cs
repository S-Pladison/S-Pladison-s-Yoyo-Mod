using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public sealed class YoyoLocalIFrameGlobalProjectile : GlobalProjectile
    {
        public const int DefaultLocalIFrameValue = 10;
        public const float DefaultTopSpeedValue = 10;

        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
            => lateInstantiation && proj.IsYoyo() && !proj.IsCounterweight();

        public override void SetDefaults(Projectile proj)
        {
            TryApplyDefaultYoyoLocalIFrame(proj);
        }

        private static bool TryApplyDefaultYoyoLocalIFrame(Projectile proj)
        {
            if (proj.usesLocalNPCImmunity || proj.usesIDStaticNPCImmunity)
                return false;

            if (ProjectileID.Sets.YoyosTopSpeed[proj.type] <= 0)
                return false;

            proj.usesLocalNPCImmunity = true;
            proj.localNPCHitCooldown = (int)(DefaultLocalIFrameValue * proj.MaxUpdates * (DefaultTopSpeedValue / ProjectileID.Sets.YoyosTopSpeed[proj.type]));

            proj.usesIDStaticNPCImmunity = false;
            proj.idStaticNPCHitCooldown = 0;

            return true;
        }
    }
}