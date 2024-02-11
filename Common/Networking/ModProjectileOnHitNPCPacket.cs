using System.IO;
using Terraria;
using Terraria.ID;

namespace SPYoyoMod.Common.Networking
{
    public class ModProjectileOnHitNPCPacket : ProjectilePacket
    {
        public readonly byte NPCWhoAmI;
        public readonly short NPCType;

        public ModProjectileOnHitNPCPacket() { }

        public ModProjectileOnHitNPCPacket(int projIdentity, int projType, int npcWhoAmI, int npcType) : base(projIdentity, projType)
        {
            NPCWhoAmI = (byte)npcWhoAmI;
            NPCType = (short)npcType;
        }

        protected override void PostSend(BinaryWriter writer, Projectile proj)
        {
            writer.Write(NPCType);
            writer.Write(NPCWhoAmI);
        }

        protected override void PostReceive(BinaryReader reader, int sender, Projectile proj)
        {
            var npcType = reader.ReadInt16();
            var npcWhoAmI = reader.ReadByte();

            if (proj is null) return;

            var npc = Main.npc[npcWhoAmI];

            if (npc.type != npcType) return;

            proj.ModProjectile?.OnHitNPC(npc, default, 0);

            if (Main.netMode == NetmodeID.Server)
            {
                new ModProjectileOnHitNPCPacket(proj.identity, proj.type, npcWhoAmI, npcType).Send(-1, sender);
            }
        }
    }
}