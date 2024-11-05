using System.IO;

namespace SPYoyoMod.Core.Netcode
{
    public abstract class NetPacket
    {
        public abstract void Send(BinaryWriter writer, params object[] context);
        public abstract void Receive(BinaryReader reader, int sender);
    }
}