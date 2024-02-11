using System.IO;
using System.Linq;
using Terraria;

namespace SPYoyoMod.Common.Networking
{
    public abstract class ProjectilePacket : NetPacket
    {
        public readonly short ProjIdentity;
        public readonly short ProjType;

        public ProjectilePacket() { }

        public ProjectilePacket(int projIdentity, int projType)
        {
            ProjIdentity = (short)projIdentity;
            ProjType = (short)projType;
        }

        public sealed override void Send(BinaryWriter writer)
        {
            writer.Write(ProjIdentity);
            writer.Write(ProjType);

            var proj = Main.projectile.FirstOrDefault(p => p.identity == ProjIdentity && p.type == ProjType);

            PostSend(writer, proj);
        }

        public sealed override void Receive(BinaryReader reader, int sender)
        {
            var projIdentity = reader.ReadInt16();
            var projType = reader.ReadInt16();

            var proj = Main.projectile.FirstOrDefault(p => p.identity == projIdentity && p.type == projType);

            PostReceive(reader, sender, proj);
        }

        protected abstract void PostSend(BinaryWriter writer, Projectile proj);
        protected abstract void PostReceive(BinaryReader reader, int sender, Projectile proj);
    }
}