using System.IO;

namespace SPYoyoMod.Common.Networking
{
    public abstract class NetPacket
    {
        public abstract void Send(BinaryWriter writer);
        public abstract void Receive(BinaryReader reader, int sender);

        public void Send(int to = -1, int from = -1)
        {
            NetHandler.Send(this, to, from);
        }
    }
}