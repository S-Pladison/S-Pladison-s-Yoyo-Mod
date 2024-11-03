using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Core.Netcode;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    // [
    //   IsBossOrRelated, IsBoss, IsBossLimb, IsMiniBoss and IsChild taken from:
    //   https://github.com/SamsonAllen13/ClickerClass/blob/master/Utilities/NPCHelper.cs
    //   https://github.com/SamsonAllen13/ClickerClass/blob/master/Utilities/NPCHelper_Bosses.cs
    // ]

    public static class NPCUtils
    {
        /// <summary>
        /// Кол-во полученного урона от йо-йо и всего, что с ним связано.
        /// </summary>
        public static uint TotalDamageTakenFromYoyos(this NPC npc)
            => npc.TryGetGlobalNPC<TotalDamageFromYoyosGlobalNPC>(out var globalProj) ? globalProj.TotalDamage : 0;

        /// <summary>
        /// Связан ли этот НПС как то с боссом или мини-боссом. Этом может быть и сам босс, в случае Скелетрона - рука и т.д.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBossOrRelated(this NPC npc)
            => npc.IsBoss() || npc.IsBossLimb() || npc.IsMiniBoss();

        /// <summary>
        /// Является ли этот NPC боссом. В большинстве случаем, на это влияет значение поля npc.boss или NPCID.Sets.ShouldBeCountedAsBoss[npc.type],
        /// но есть исключения.
        /// </summary>
        public static bool IsBoss(this NPC npc)
        {
            var type = npc.type;

            if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[type])
                return true;

            switch (type)
            {
                // Eater of Worlds
                case NPCID.EaterofWorldsHead:
                case NPCID.EaterofWorldsBody:
                case NPCID.EaterofWorldsTail:
                // Misc
                case NPCID.DungeonGuardian:
                    return true;
                default:
                    break;
            }

            return npc.IsChild(out NPC parent) && parent.whoAmI != npc.whoAmI && parent.IsBoss();
        }

        /// <summary>
        /// Является ли этот NPC частью/конечностью босса.
        /// </summary>
        public static bool IsBossLimb(this NPC npc)
        {
            switch (npc.type)
            {
                // Eater of Worlds
                case NPCID.EaterofWorldsHead:
                case NPCID.EaterofWorldsBody:
                case NPCID.EaterofWorldsTail:
                // Skeletron
                case NPCID.SkeletronHand:
                // Skeletron Prime
                case NPCID.PrimeCannon:
                case NPCID.PrimeLaser:
                case NPCID.PrimeSaw:
                case NPCID.PrimeVice:
                // Golem
                case NPCID.GolemHead:
                case NPCID.GolemHeadFree:
                case NPCID.GolemFistLeft:
                case NPCID.GolemFistRight:
                // Pirate Ship
                case NPCID.PirateShipCannon:
                // Martian Saucer
                case NPCID.MartianSaucerCannon:
                case NPCID.MartianSaucerTurret:
                case NPCID.MartianSaucer:
                // Moon Lord
                case NPCID.MoonLordHead:
                case NPCID.MoonLordHand:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Является ли этот NPC мини-боссом.
        /// </summary>
        public static bool IsMiniBoss(this NPC npc)
        {
            switch (npc.type)
            {
                // Biomes
                case NPCID.SandElemental:
                case NPCID.IceGolem:
                case NPCID.Paladin:
                case NPCID.Mothron:
                case NPCID.MartianSaucerCore:
                // Events
                case NPCID.PirateShip:
                case NPCID.IceQueen:
                case NPCID.SantaNK1:
                case NPCID.Everscream:
                case NPCID.Pumpking:
                case NPCID.MourningWood:
                case NPCID.DD2Betsy:
                case NPCID.DD2DarkMageT1:
                case NPCID.DD2DarkMageT3:
                case NPCID.DD2OgreT2:
                case NPCID.DD2OgreT3:
                // Misc
                case NPCID.WyvernHead:
                case NPCID.GoblinSummoner:
                case NPCID.PirateCaptain:
                case NPCID.HeadlessHorseman:
                case NPCID.Nailhead:
                    return true;
                default:
                    break;
            }

            switch (npc.aiStyle)
            {
                case NPCAIStyleID.BiomeMimic:
                    return true;
                default:
                    break;
            }

            return npc.IsChild(out NPC parent) && parent.whoAmI != npc.whoAmI && parent.IsMiniBoss();
        }

        /// <summary>
        /// Проверяет, привязан ли этот NPC к пулу здоровья другого NPC.
        /// </summary>
        public static bool IsChild(this NPC npc, out NPC parent)
        {
            var child = npc.realLife >= 0 && npc.realLife <= Main.maxNPCs && npc.realLife != npc.whoAmI;
            parent = (child ? Main.npc[npc.realLife] : null);
            return child;
        }

        private sealed class TakeDamageFromYoyoPacket : NetPacket
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
                    globalNPC.TotalDamage = (globalNPC.TotalDamage > uint.MaxValue - damage) ? uint.MaxValue : globalNPC.TotalDamage + damage;

                if (Main.netMode == NetmodeID.Server)
                    NetHandler.Send<TakeDamageFromYoyoPacket>(null, sender, npcWhoAmI, damage);
            }
        }

        private sealed class RequestTotalDamageFromYoyosPacket : NetPacket
        {
            public override void Send(BinaryWriter writer, params object[] context) { }

            public override void Receive(BinaryReader reader, int sender)
            {
                if (Main.netMode == NetmodeID.Server)
                    NetHandler.Send<GetTotalDamageFromYoyosPacket>(sender, null);
            }
        }

        private sealed class GetTotalDamageFromYoyosPacket : NetPacket
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

        private sealed class TotalDamageFromYoyosGlobalNPC : GlobalNPC
        {
            public uint TotalDamage { get; set; }
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
            }

            private static void BeforeStrikeNPCByProjectile(Projectile proj, NPC npc, NPC.HitInfo hit)
            {
                if (!proj.IsYoyoOrRelated() || npc.immortal)
                    return;

                if (!npc.TryGetGlobalNPC(out TotalDamageFromYoyosGlobalNPC globalNPC))
                    return;

                globalNPC.TotalDamage = (globalNPC.TotalDamage > uint.MaxValue - (uint)hit.Damage) ? uint.MaxValue : globalNPC.TotalDamage + (uint)hit.Damage;

                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetHandler.Send<TakeDamageFromYoyoPacket>(null, null, (byte)npc.whoAmI, (uint)hit.Damage);
            }
        }

        private sealed class TotalDamageFromYoyosPlayer : ModPlayer
        {
            public override void PlayerConnect()
            {
                NetHandler.Send<RequestTotalDamageFromYoyosPacket>(null, null);
            }
        }
    }
}