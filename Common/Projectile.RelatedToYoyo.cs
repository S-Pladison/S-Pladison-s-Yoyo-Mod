using SPYoyoMod.Utils;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SPYoyoMod.Common
{
    public sealed class RelatedToYoyoGlobalProjectile : GlobalProjectile
    {
        public bool RelatedToYoyo { get; private set; }
        public override bool InstancePerEntity { get => true; }

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
}
