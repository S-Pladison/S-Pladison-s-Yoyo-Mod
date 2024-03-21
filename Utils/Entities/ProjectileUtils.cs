using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    public static partial class EntityExtensions
    {
        public static bool IsYoyo(this Projectile proj) { return proj.aiStyle.Equals(99); }
        public static bool IsCounterweight(this Projectile proj) { return proj.IsYoyo() && proj.counterweight; }

        public static void DefaultToVisualEffect(this Projectile proj)
        {
            proj.width = 16;
            proj.height = 16;
            proj.timeLeft = 60;
            proj.friendly = true;
            proj.penetrate = -1;
            proj.ignoreWater = true;
            proj.tileCollide = false;

            proj.DamageType = DamageClass.Generic;
        }

        public static void MoveTo(this Projectile proj, Vector2 position, float maxVelocity, float velocityWeight)
        {
            var direction = position - proj.Center;
            direction *= maxVelocity / direction.Length();

            proj.velocity *= 1f - velocityWeight;
            proj.velocity += direction * velocityWeight;
        }
    }
}