using System.IO;
using Terraria.ModLoader;

namespace SPYoyoMod.Core.Netcode
{
    public abstract class NetPacket : ILoadable
    {
        private static byte _idGenerator;

        public byte ID { get; private set; }

        public abstract void Send(BinaryWriter writer, params object[] context);
        public abstract void Receive(BinaryReader reader, int sender);

        void ILoadable.Load(Mod mod)
        {
            ID = _idGenerator++;
        }

        void ILoadable.Unload()
        {
            _idGenerator = 0;
        }
    }
}