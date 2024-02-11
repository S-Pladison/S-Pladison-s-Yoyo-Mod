using System.IO;

namespace SPYoyoMod.Common.Networking
{
    public abstract class NetPacket
    {
        public abstract void Send(BinaryWriter writer);
        public abstract void Receive(BinaryReader reader, int sender);
    }
}