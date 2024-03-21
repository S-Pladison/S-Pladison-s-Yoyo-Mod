using Microsoft.Xna.Framework;
using MonoMod.Cil;
using SPYoyoMod.Common.Networking;
using SPYoyoMod.Utils;
using System;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using static Mono.Cecil.Cil.OpCodes;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public class EmbraceOfRainItem : ModItem
    {
        public override string Texture => ModAssets.ItemsPath + "Bearing";

        public override Color? GetAlpha(Color lightColor)
        {
            return Main.DiscoColor;
        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 36;
            Item.height = 34;

            Item.rare = ItemRarityID.LightPurple;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }

        public override void UpdateEquip(Player player)
        {
            player.SetEffectFlag<EmbraceOfRainItem>();
        }
    }

    public class EmbraceOfRainGlobalNPC : GlobalNPC
    {
        private class EmbraceOfRainDamagePacket : NPCPacket
        {
            public readonly short Damage;

            public EmbraceOfRainDamagePacket() { }

            public EmbraceOfRainDamagePacket(NPC npc, int damage) : this(npc.whoAmI, npc.type, damage) { }

            public EmbraceOfRainDamagePacket(int npcWhoAmI, int npcType, int damage) : base(npcWhoAmI, npcType)
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

                if (npc is null || !npc.TryGetGlobalNPC(out EmbraceOfRainGlobalNPC globalNPC)) return;

                globalNPC.DamageDoneByYoyo += damage;

                if (Main.netMode == NetmodeID.Server)
                {
                    new EmbraceOfRainDamagePacket(npc, damage).Send(-1, sender);
                }
            }
        }

        private static bool isRerolledLoot;

        public int DamageDoneByYoyo { get; private set; }
        public override bool InstancePerEntity { get => true; }

        public override void Load()
        {
            On_ItemDropResolver.ResolveRule += IncreaseLoot;

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

        public bool CanRerollLoot(NPC npc)
        {
            return DamageDoneByYoyo > (npc.lifeMax / 3) || DamageDoneByYoyo < 0;
        }

        // The main reason why we don't use OnHitByProjectile is instant npc kill,
        // NPCLoot is called before OnHitByProjectile...
        // ModifyHitByProjectile will have incorrect damage values
        private static void BeforeStrikeNPCByProjectile(Projectile proj, NPC npc, NPC.HitInfo hit)
        {
            if (!proj.TryGetOwner(out Player owner) || !owner.GetEffectFlag<EmbraceOfRainItem>())
                return;

            if (!proj.IsYoyoOrRelated() || npc.immortal)
                return;

            if (!npc.TryGetGlobalNPC(out EmbraceOfRainGlobalNPC globalNPC))
                return;

            globalNPC.DamageDoneByYoyo += hit.Damage;

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                new EmbraceOfRainDamagePacket(npc, hit.Damage).Send();
            }
        }

        private static ItemDropAttemptResult IncreaseLoot(On_ItemDropResolver.orig_ResolveRule orig, ItemDropResolver @this, IItemDropRule rule, DropAttemptInfo info)
        {
            var result = orig(@this, rule, info);

            if (result.State != ItemDropAttemptResultState.FailedRandomRoll
                || isRerolledLoot)
                return result;

            if (info.player is null
                || info.npc is null
                || !info.npc.TryGetGlobalNPC(out EmbraceOfRainGlobalNPC embraceOfRainGlobalNPC)
                || !embraceOfRainGlobalNPC.CanRerollLoot(info.npc))
                return result;

            isRerolledLoot = true;

            try
            {
                /*var rerollResult = orig(@this, rule, info);
                isRerolledLoot = false;
                return rerollResult;*/

                var rerollResult = orig(@this, rule, info);

                while (rerollResult.State == ItemDropAttemptResultState.FailedRandomRoll)
                {
                    rerollResult = orig(@this, rule, info);
                }

                isRerolledLoot = false;
                return rerollResult;
            }
            catch (Exception)
            {
                // ...
            }

            isRerolledLoot = false;
            return result;
        }
    }

    /*public class EmbraceOfRainPlayerLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            throw new NotImplementedException();
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            throw new NotImplementedException();
        }
    }

    public class EmbraceOfRainRenderTargetContent : RenderTargetContent
    {
        public override Point Size => throw new NotImplementedException();

        public override void DrawToTarget()
        {
            throw new NotImplementedException();
        }
    }*/
}