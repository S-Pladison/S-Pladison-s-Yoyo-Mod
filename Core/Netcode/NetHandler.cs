using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Core.Netcode
{
    public sealed class NetHandler : ILoadable
    {
        private static Mod _mod;
        private static List<NetPacket> _packetList;
        private static Dictionary<Type, byte> _packetIdByTypeDict;

        public static void Receive(BinaryReader reader, int sender)
        {
            GetPacketById(reader.ReadByte()).Receive(reader, sender);
        }

        public static void Send<T>(int? toClient, int? ignoreClient, params object[] context) where T : NetPacket
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            var type = typeof(T);
            var id = _packetIdByTypeDict[type];
            var packet = GetPacketById(id);
            var modPacket = _mod.GetPacket();

            modPacket.Write(id);
            packet.Send(modPacket, context);

            modPacket.Send(toClient ?? -1, ignoreClient ?? -1);
        }

        private static void RegisterPackets()
        {
            _packetList.Clear();
            _packetIdByTypeDict.Clear();

            foreach (var type in AssemblyManager.GetLoadableTypes(_mod.Code).Where(
                t => !t.IsAbstract &&
                t.IsSubclassOf(typeof(NetPacket)) &&
                t.GetConstructors().Any(c => c.GetParameters().Length == 0)
            ).OrderBy(t => t.Name))
            {
                var packet = Activator.CreateInstance(type) as NetPacket;
                var id = (byte)(_packetList.Count + 1);

                _packetList.Add(packet);
                _packetIdByTypeDict[type] = id;

                _mod.Logger.Debug($"Registered NetPacket::{type.Name} with ID::{id}");
            }
        }

        private static NetPacket GetPacketById(byte id)
        {
            if (id == 0 || id > _packetList.Count)
                throw new NotImplementedException();

            return _packetList[id - 1];
        }

        void ILoadable.Load(Mod mod)
        {
            _mod = mod;
            _packetList = [];
            _packetIdByTypeDict = [];

            RegisterPackets();
        }

        void ILoadable.Unload()
        {
            _packetIdByTypeDict = null;
            _packetList = null;
            _mod = null;
        }
    }
}