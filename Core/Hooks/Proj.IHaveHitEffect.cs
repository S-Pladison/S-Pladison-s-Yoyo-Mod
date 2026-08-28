using SPYoyoMod.Core.Netcode;
using SPYoyoMod.Utils;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using IHook = SPYoyoMod.Core.Hooks.IHaveHitEffectProjectile;

namespace SPYoyoMod.Core.Hooks
{
    /// <summary>
    /// Позволяет создать эффект при попадании снаряда по NPC (пыль, частицы и т.п.).<br/>
    /// В отличии от <see cref="ModProjectile.OnHitNPC(NPC, NPC.HitInfo, int)"/>, вызов происходит на всех клиентах и на сервере.<br/>
    /// Интерфейс относится к следующим классам: <see cref="ModProjectile"/> и <see cref="GlobalProjectile"/><br/>
    /// </summary>
    public interface IHaveHitEffectProjectile
    {
        internal static readonly GlobalHookList<GlobalProjectile> _hook =
            ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(i => ((IHook)i).HitEffect));

        /// <summary>
        /// Позволяет создать эффект при попадании снаряда по NPC (пыль, частицы и т.п.).
        /// Вызывается на всех клиентах и на сервере.
        /// </summary>
        void HitEffect(Projectile proj, NPC target, NPC.HitInfo hit);

        private static bool InvokeHitEffect(Projectile proj, NPC target, in NPC.HitInfo hit)
        {
            var any = false;

            if (proj.ModProjectile is IHook modProj)
            {
                any = true;
                modProj.HitEffect(proj, target, hit);
            }

            foreach (IHook g in IHook._hook.Enumerate(proj))
            {
                any = true;
                g.HitEffect(proj, target, hit);
            }

            return any;
        }

        // Локальный игрок владеет снарядом, либо это серверный снаряд на сервере
        private static bool IsOwner(Projectile proj)
            => proj.owner == Main.myPlayer || (Main.netMode == NetmodeID.Server && proj.owner == 255);

        private sealed class HaveHitEffectProjectileImplementation : GlobalProjectile
        {
            private static GlobalProjectile[] _hitEffectGlobals;

            private static IReadOnlyList<GlobalProjectile> HitEffectGlobals
            {
                get
                {
                    if (_hitEffectGlobals is not null)
                        return _hitEffectGlobals;

                    var globals = new List<GlobalProjectile>();

                    foreach (var global in ModContent.GetContent<GlobalProjectile>())
                    {
                        if (global is IHook)
                            globals.Add(global);
                    }

                    return _hitEffectGlobals = [.. globals];
                }
            }

            public override void Unload()
            {
                _hitEffectGlobals = null;
            }

            public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
            {
                if (!lateInstantiation)
                    return false;

                if (proj.ModProjectile is IHook)
                    return true;

                foreach (var global in HitEffectGlobals)
                {
                    if (!global.ConditionallyAppliesToEntities)
                        return true;

                    if (global.AppliesToEntity(proj, lateInstantiation: false) || global.AppliesToEntity(proj, lateInstantiation: true))
                        return true;
                }

                return false;
            }

            public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
            {
                if (!InvokeHitEffect(proj, target, hit))
                    return;

                if (IsOwner(proj))
                    NetHandler.Send<ProjectileHitEffectPacket>(null, null, (byte)proj.owner, (ushort)proj.identity, (byte)target.whoAmI, hit);
            }
        }

        private sealed class ProjectileHitEffectPacket : NetPacket
        {
            public override void Send(BinaryWriter writer, params object[] context)
            {
                writer.Write((byte)context[0]); //< owner
                writer.Write((ushort)context[1]); //< identity
                writer.Write((byte)context[2]); //< npcWhoAmI

                WriteHitInfo(writer, (NPC.HitInfo)context[3]);
            }

            public override void Receive(BinaryReader reader, int sender)
            {
                var owner = reader.ReadByte();
                var identity = reader.ReadUInt16();
                var npcWhoAmI = reader.ReadByte();
                var hit = ReadHitInfo(reader);

                if (Main.netMode == NetmodeID.Server)
                    NetHandler.Send<ProjectileHitEffectPacket>(null, sender, owner, identity, npcWhoAmI, hit);

                var proj = Main.ActiveProjectiles.FindByIdentity(identity);
                var npc = npcWhoAmI < Main.maxNPCs ? Main.npc[npcWhoAmI] : null;

                if (proj is null || proj.owner != owner || npc is null || !npc.active)
                    return;

                if (IsOwner(proj))
                    return;

                InvokeHitEffect(proj, npc, hit);
            }

            private static void WriteHitInfo(BinaryWriter writer, in NPC.HitInfo hit)
            {
                writer.Write(hit.Damage);
                writer.Write(hit.SourceDamage);
                writer.Write(hit.Knockback);
                writer.Write((sbyte)hit.HitDirection);
                writer.Write(hit.Crit);
                writer.Write(hit.InstantKill);
                writer.Write(hit.HideCombatText);
                writer.Write((ushort)(hit.DamageType?.Type ?? 0));
            }

            private static NPC.HitInfo ReadHitInfo(BinaryReader reader)
            {
                return new NPC.HitInfo
                {
                    Damage = reader.ReadInt32(),
                    SourceDamage = reader.ReadInt32(),
                    Knockback = reader.ReadSingle(),
                    HitDirection = reader.ReadSByte(),
                    Crit = reader.ReadBoolean(),
                    InstantKill = reader.ReadBoolean(),
                    HideCombatText = reader.ReadBoolean(),
                    DamageType = DamageClassLoader.GetDamageClass(reader.ReadUInt16()) ?? DamageClass.Default
                };
            }
        }
    }
}
