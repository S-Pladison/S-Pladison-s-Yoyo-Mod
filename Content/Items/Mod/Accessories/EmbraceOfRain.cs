using Microsoft.Xna.Framework;
using SPYoyoMod.Utils;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

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
        private static bool isRerolledLoot;

        public int DamageDoneByYoyo { get; private set; }
        public override bool InstancePerEntity { get => true; }

        public override void Load()
        {
            On_ItemDropResolver.ResolveRule += IncreaseLoot;
        }

        public override void OnHitByProjectile(NPC npc, Projectile proj, NPC.HitInfo hit, int damageDone)
        {
            if (!proj.TryGetOwner(out Player owner) || !owner.GetEffectFlag<EmbraceOfRainItem>())
                return;

            if (!proj.IsYoyoOrRelated() || npc.immortal)
                return;

            DamageDoneByYoyo += damageDone;
        }

        public bool CanRerollLoot(NPC npc)
        {
            return DamageDoneByYoyo > (npc.lifeMax / 3) || DamageDoneByYoyo < 0;
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            var flag = DamageDoneByYoyo != 0;

            bitWriter.WriteBit(flag);

            if (flag)
            {
                binaryWriter.Write(DamageDoneByYoyo);
            }
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            var flag = bitReader.ReadBit();

            if (flag)
            {
                DamageDoneByYoyo = binaryReader.ReadInt32();
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
                var rerollResult = orig(@this, rule, info);
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
}