using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Core.Netcode;
using SPYoyoMod.Utils;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Yoyos
{
    public sealed class TotalDamageFromYoyosGlobalNPC : GlobalNPC
    {
        public uint TotalDamage { get; private set; }
        public override bool InstancePerEntity { get => true; }

        public override void Load()
        {
            // Основная причина, по которой мы не используем OnHitByProjectile - мгновенное убийство NPC (последнее попадание перед убийством не учитывается)
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
                        i => i.MatchCall(typeof(NPCKillAttempt).GetConstructor(BindingFlags.Public | BindingFlags.Instance, [typeof(NPC)])))) return;

                c.Emit(OpCodes.Ldarg, 0); // proj
                c.Emit(OpCodes.Ldloc, 26); // npc
                c.Emit(OpCodes.Ldloc, 39); // strike (hit)
                c.EmitDelegate(BeforeStrikeNPCByProjectile);
            };

            // Отправляем с сервера подключенному игроку информацию обо всех NPC с их общим полученным уроном
            ModEvents.OnPlayerConnect += (player) =>
            {
                if (Main.netMode == NetmodeID.Server)
                    NetHandler.Send<NPCTotalDamageFromYoyosPacket>(player.whoAmI, null);
            };
        }

        private static void BeforeStrikeNPCByProjectile(Projectile proj, NPC npc, NPC.HitInfo hit)
        {
            if (!proj.IsYoyoOrRelated() || npc.immortal)
                return;

            if (!npc.TryGetGlobalNPC(out TotalDamageFromYoyosGlobalNPC globalNPC))
                return;

            globalNPC.TotalDamage = globalNPC.TotalDamage > uint.MaxValue - (uint)hit.Damage ? uint.MaxValue : globalNPC.TotalDamage + (uint)hit.Damage;

            if (Main.netMode != NetmodeID.SinglePlayer)
                NetHandler.Send<NPCTakeDamageFromYoyoPacket>(null, null, (byte)npc.whoAmI, (uint)hit.Damage);
        }

        private sealed class NPCTakeDamageFromYoyoPacket : NetPacket
        {
            public override void Send(BinaryWriter writer, params object[] context)
            {
                writer.Write((byte)context[0]); //< npcWhoAmI
                writer.Write((uint)context[1]); //< yoyoDamage
            }

            public override void Receive(BinaryReader reader, int sender)
            {
                var npcWhoAmI = reader.ReadByte();
                var damage = reader.ReadUInt32();

                if (Main.npc[npcWhoAmI]?.TryGetGlobalNPC<TotalDamageFromYoyosGlobalNPC>(out var globalNPC) ?? false)
                    globalNPC.TotalDamage = globalNPC.TotalDamage > uint.MaxValue - damage ? uint.MaxValue : globalNPC.TotalDamage + damage;

                if (Main.netMode == NetmodeID.Server)
                    NetHandler.Send<NPCTakeDamageFromYoyoPacket>(null, sender, npcWhoAmI, damage);
            }
        }

        private sealed class NPCTotalDamageFromYoyosPacket : NetPacket
        {
            public override void Send(BinaryWriter writer, params object[] context)
            {
                var npcWithAnyDamage = new List<(byte whoImA, uint totalDamage)>();

                foreach (var npc in Main.ActiveNPCs)
                {
                    if (!npc.TryGetGlobalNPC<TotalDamageFromYoyosGlobalNPC>(out var globalNPC) || globalNPC.TotalDamage == 0)
                        continue;

                    npcWithAnyDamage.Add(((byte)npc.whoAmI, globalNPC.TotalDamage));
                }

                writer.Write((byte)npcWithAnyDamage.Count);

                foreach (var (whoImA, totalDamage) in npcWithAnyDamage)
                {
                    writer.Write(whoImA);
                    writer.Write(totalDamage);
                }
            }

            public override void Receive(BinaryReader reader, int sender)
            {
                var npcWithAnyDamageCount = reader.ReadByte();

                for (var index = 0; index < npcWithAnyDamageCount; index++)
                {
                    var npcWhoAmI = reader.ReadByte();
                    var totalDamage = reader.ReadUInt32();
                    var npc = Main.npc[npcWhoAmI];

                    if (npc?.TryGetGlobalNPC<TotalDamageFromYoyosGlobalNPC>(out var globalNPC) ?? false)
                    {
                        globalNPC.TotalDamage = totalDamage;
                    }
                }
            }
        }
    }
}
