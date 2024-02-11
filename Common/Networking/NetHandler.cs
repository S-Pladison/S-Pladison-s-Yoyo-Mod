using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Networking
{
    // Source: https://github.com/SamsonAllen13/ClickerClass/blob/master/Core/Netcode/NetHandler.cs
    public sealed class NetHandler : ILoadable
    {
        private readonly List<NetPacket> packetInstances;
        private readonly Dictionary<Type, byte> ids;

        private Mod mod;

        public NetHandler()
        {
            packetInstances = new List<NetPacket>();
            ids = new Dictionary<Type, byte>();
        }

        void ILoadable.Load(Mod mod)
        {
            this.mod = mod;

            foreach (var type in AssemblyManager.GetLoadableTypes(mod.Code).Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(NetPacket))))
            {
                var packet = Activator.CreateInstance(type) as NetPacket;

                ids[type] = (byte)packetInstances.Count;

                packetInstances.Add(packet);
            }
        }

        void ILoadable.Unload()
        {
            packetInstances.Clear();
            ids.Clear();
        }

        public void HandlePackets(BinaryReader reader, int sender)
        {
            try
            {
                var id = reader.ReadByte();

                if (id >= packetInstances.Count) return;

                packetInstances[id].Receive(reader, sender);
            }
            catch (Exception)
            {
                // ...
            }
        }

        /// <summary>
        /// Sends a packet of this given type.
        /// </summary>
        /// <param name="to">The client whoAmI, -1 if everyone.</param>
        /// <param name="from">The client the packet originated from.</param>
        public static void Send<T>(T packet, int to = -1, int from = -1) where T : NetPacket
        {
            if (Main.netMode == NetmodeID.SinglePlayer) return;

            var netHandler = ModContent.GetInstance<NetHandler>();
            var packetType = packet.GetType();
            var modPacket = netHandler.mod.GetPacket();

            modPacket.Write(netHandler.ids[packetType]);
            packet.Send(modPacket);

            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    // To/from doesn't matter for client, it always goes to server
                    modPacket.Send();
                }
                else if (to != -1) // Server and specific client
                {
                    modPacket.Send(to, from);
                }
                else // Server and broadcast
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (i != from && Netplay.Clients[i].State >= 10)
                        {
                            modPacket.Send(i);
                        }
                    }
                }
            }
            catch
            {
                // ...
            }
        }
    }
}