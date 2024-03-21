using System.IO;
using Terraria;

namespace SPYoyoMod.Common.Networking
{
    public abstract class NPCPacket : NetPacket
    {
        public readonly byte NPCWhoAmI;
        public readonly short NPCType;

        public NPCPacket() { }

        public NPCPacket(int npcWhoAmI, int npcType)
        {
            NPCWhoAmI = (byte)npcWhoAmI;
            NPCType = (short)npcType;
        }

        public sealed override void Send(BinaryWriter writer)
        {
            writer.Write(NPCWhoAmI);
            writer.Write(NPCType);

            NPC npc = null;

            if (NPCWhoAmI >= 0 && NPCWhoAmI < Main.maxNPCs && Main.npc[NPCWhoAmI].type == NPCType)
            {
                npc = Main.npc[NPCWhoAmI];
            }

            PostSend(writer, npc);
        }

        public sealed override void Receive(BinaryReader reader, int sender)
        {
            var npcWhoAmI = reader.ReadByte();
            var npcType = reader.ReadInt16();

            NPC npc = null;

            if (npcWhoAmI >= 0 && npcWhoAmI < Main.maxNPCs && Main.npc[npcWhoAmI].type == npcType)
            {
                npc = Main.npc[npcWhoAmI];
            }

            PostReceive(reader, sender, npc);
        }

        protected abstract void PostSend(BinaryWriter writer, NPC npc);
        protected abstract void PostReceive(BinaryReader reader, int sender, NPC npc);
    }
}