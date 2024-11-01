using System.IO;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace SPYoyoMod.Core.Netcode
{
    public sealed class NetHandler : ILoadable
    {
        private static Mod _mod;
        private static List<NetPacket> _packets = [];

        public static void Receive(BinaryReader reader, int sender)
        {
            var packet = _packets[reader.ReadByte()];
            packet.Receive(reader, sender);
        }

        public static void Send<T>(int? toClient, int? ignoreClient, params object[] context) where T : NetPacket
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            var modPacket = _mod.GetPacket();
            var packet = _packets[ModContent.GetInstance<T>().ID];

            modPacket.Write(packet.ID);
            packet.Send(modPacket, context);

            modPacket.Send(toClient ?? -1, ignoreClient ?? -1);
        }

        private static void RegisterPackets()
        {
            foreach (var type in _mod.GetContent<NetPacket>())
                _packets.Add(type);
        }

        void ILoadable.Load(Mod mod)
        {
            _mod = mod;

            ModEvents.OnPostSetupContent += RegisterPackets;
        }

        void ILoadable.Unload()
        {
            ModEvents.OnPostSetupContent -= RegisterPackets;

            _packets = null;
            _mod = null;
        }
    }
}