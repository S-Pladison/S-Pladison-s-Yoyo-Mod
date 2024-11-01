using System.IO;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using System;

namespace SPYoyoMod.Core.Netcode
{
    public sealed class NetHandler : ILoadable
    {
        private static Mod _mod;
        private static List<NetPacket> _packets;

        public static void Receive(BinaryReader reader, int sender)
        {
            var id = reader.ReadByte();

            if (id == 0)
                throw new NotImplementedException();

            var packet = _packets[id - 1];
            packet.Receive(reader, sender);
        }

        public static void Send<T>(int? toClient, int? ignoreClient, params object[] context) where T : NetPacket
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            var packet = ModContent.GetInstance<T>();

            if (packet.ID == 0)
                throw new NotImplementedException();

            var modPacket = _mod.GetPacket();

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
            _packets = [];

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