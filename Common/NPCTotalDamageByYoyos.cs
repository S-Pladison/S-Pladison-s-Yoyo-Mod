using MonoMod.Cil;
using SPYoyoMod.Common.Networking;
using SPYoyoMod.Utils;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static Mono.Cecil.Cil.OpCodes;

namespace SPYoyoMod.Common
{
    public sealed class NPCTotalDamageByYoyos : GlobalNPC
    {
        private class NPCTotalDamageByYoyosPacket : NPCPacket
        {
            public readonly short Damage;

            public NPCTotalDamageByYoyosPacket() { }

            public NPCTotalDamageByYoyosPacket(NPC npc, int damage) : this(npc.whoAmI, npc.type, damage) { }

            public NPCTotalDamageByYoyosPacket(int npcWhoAmI, int npcType, int damage) : base(npcWhoAmI, npcType)
            {
                Damage = (short)damage;
            }

            protected override void PostSend(BinaryWriter writer, NPC npc)
            {
                writer.Write(Damage);
            }

            protected override void PostReceive(BinaryReader reader, int sender, NPC npc)
            {
                var damage = reader.ReadInt16();

                if (npc is null || !npc.TryGetGlobalNPC(out NPCTotalDamageByYoyos globalNPC)) return;

                globalNPC.TotalDamage += damage;

                if (Main.netMode == NetmodeID.Server)
                {
                    new NPCTotalDamageByYoyosPacket(npc, damage).Send(-1, sender);
                }
            }
        }

        public int TotalDamage { get; private set; }
        public override bool InstancePerEntity { get => true; }

        public override void Load()
        {
            IL_Projectile.Damage += (il) =>
            {
                var c = new ILCursor(il);

                // NPCKillAttempt attempt = new NPCKillAttempt(nPC);

                // IL_31ec: ldloca.s 40
                // IL_31ee: ldloc.s 26
                // IL_31f0: call instance void Terraria.DataStructures.NPCKillAttempt::.ctor(class Terraria.NPC)

                if (!c.TryGotoNext(MoveType.After,
                        i => i.MatchLdloca(40),
                        i => i.MatchLdloc(26),
                        i => i.MatchCall(typeof(NPCKillAttempt).GetConstructor(BindingFlags.Public | BindingFlags.Instance, new[] { typeof(NPC) })))) return;

                c.Emit(Ldarg, 0); // proj
                c.Emit(Ldloc, 26); // npc
                c.Emit(Ldloc, 39); // strike (hit)
                c.EmitDelegate(BeforeStrikeNPCByProjectile);
            };
        }

        // The main reason why we don't use OnHitByProjectile is npc instant kill (last hit before kill does not count)
        // ModifyHitByProjectile will have incorrect damage values
        private static void BeforeStrikeNPCByProjectile(Projectile proj, NPC npc, NPC.HitInfo hit)
        {
            if (!proj.IsYoyoOrRelated() || npc.immortal)
                return;

            if (!npc.TryGetGlobalNPC(out NPCTotalDamageByYoyos globalNPC))
                return;

            globalNPC.TotalDamage += hit.Damage;

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                new NPCTotalDamageByYoyosPacket(npc, hit.Damage).Send();
            }
        }
    }
}