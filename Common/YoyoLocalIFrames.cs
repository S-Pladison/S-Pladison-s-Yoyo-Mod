using SPYoyoMod.Utils;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public sealed class YoyoLocalIFramesGlobalProjectile : GlobalProjectile
    {
        public const int DefaulLocalIFrameValue = 10;

        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
            => lateInstantiation && proj.IsYoyo() && !proj.IsCounterweight();

        public override void SetDefaults(Projectile proj)
        {
            if (proj.usesLocalNPCImmunity || proj.usesIDStaticNPCImmunity)
                return;

            ApplyDefaultYoyoLocalIFrame(proj);
        }

        private static void ApplyDefaultYoyoLocalIFrame(Projectile proj)
        {
            proj.usesLocalNPCImmunity = true;
            proj.localNPCHitCooldown = DefaulLocalIFrameValue * proj.MaxUpdates;

            proj.usesIDStaticNPCImmunity = false;
            proj.idStaticNPCHitCooldown = 0;
        }
    }
}