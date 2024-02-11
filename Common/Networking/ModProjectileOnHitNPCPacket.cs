using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace SPYoyoMod.Common.Networking
{
    public class ModProjectileOnHitNPCPacket : NetPacket
    {
        public readonly short ProjIdentity;
        public readonly short ProjType;

        public readonly byte NPCWhoAmI;
        public readonly short NPCType;

        public ModProjectileOnHitNPCPacket() { }

        public ModProjectileOnHitNPCPacket(Projectile proj, NPC npc) : this(proj.identity, proj.type, npc.whoAmI, npc.type) { }

        public ModProjectileOnHitNPCPacket(int projIdentity, int projType, int npcWhoAmI, int npcType)
        {
            ProjIdentity = (short)projIdentity;
            ProjType = (short)projType;
            NPCWhoAmI = (byte)npcWhoAmI;
            NPCType = (short)npcType;
        }

        public override void Send(BinaryWriter writer)
        {
            writer.Write(ProjIdentity);
            writer.Write(ProjType);
            writer.Write(NPCType);
            writer.Write(NPCWhoAmI);
        }

        public override void Receive(BinaryReader reader, int sender)
        {
            var projIdentity = reader.ReadInt16();
            var projType = reader.ReadInt16();
            var npcType = reader.ReadInt16();
            var npcWhoAmI = reader.ReadByte();

            var npc = Main.npc[npcWhoAmI];

            if (npc.type != npcType) return;

            var proj = Main.projectile.FirstOrDefault(p => p.identity == projIdentity && p.type == projType);

            if (proj is null) return;

            proj.ModProjectile?.OnHitNPC(npc, default, 0);

            if (Main.netMode == NetmodeID.Server)
            {
                new ModProjectileOnHitNPCPacket(projIdentity, projType, npcWhoAmI, npcType).Send(-1, sender);
            }
        }
    }
}