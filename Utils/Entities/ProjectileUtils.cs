using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SPYoyoMod.Utils
{
    public static partial class EntityExtensions
    {
        private class RelatedToYoyoGlobalProjectile : GlobalProjectile
        {
            public bool RelatedToYoyo { get; private set; }
            public override bool InstancePerEntity => true;

            public override void OnSpawn(Projectile proj, IEntitySource source)
            {
                if (source is not EntitySource_Parent parentSource || parentSource.Entity is not Projectile parentProj)
                    return;

                if (parentProj.IsYoyo() || parentProj.IsCounterweight())
                {
                    RelatedToYoyo = true;
                    return;
                }

                if (!parentProj.TryGetGlobalProjectile(out RelatedToYoyoGlobalProjectile parentGlobal))
                    return;

                RelatedToYoyo = parentGlobal.RelatedToYoyo;
            }

            public override void SendExtraAI(Projectile proj, BitWriter bitWriter, BinaryWriter binaryWriter)
            {
                bitWriter.WriteBit(RelatedToYoyo);
            }

            public override void ReceiveExtraAI(Projectile proj, BitReader bitReader, BinaryReader binaryReader)
            {
                RelatedToYoyo = bitReader.ReadBit();
            }
        }

        public static bool IsYoyo(this Projectile proj)
        {
            return proj.aiStyle.Equals(ProjAIStyleID.Yoyo);
        }

        public static bool IsCounterweight(this Projectile proj)
        {
            return proj.counterweight;
        }

        public static bool IsYoyoOrRelated(this Projectile proj)
        {
            return proj.IsYoyo()
                || proj.IsCounterweight()
                || (proj.TryGetGlobalProjectile(out RelatedToYoyoGlobalProjectile globalProj) && globalProj.RelatedToYoyo);
        }

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