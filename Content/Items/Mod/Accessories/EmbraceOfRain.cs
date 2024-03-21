using Microsoft.Xna.Framework;
using SPYoyoMod.Common;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public class EmbraceOfRainItem : ModItem
    {
        // Flag to stop recursion of the ItemDropResolver.ResolveRule(...)
        private static bool isRerolledLoot;

        public override string Texture => ModAssets.ItemsPath + "Bearing";

        public override Color? GetAlpha(Color lightColor)
        {
            return Main.DiscoColor;
        }

        public override void Load()
        {
            On_ItemDropResolver.ResolveRule += IncreaseLoot;
        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 36;
            Item.height = 34;

            Item.rare = ItemRarityID.LightPurple;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<PlayerEffectFlags>().SetFlag<EmbraceOfRainItem>();
            player.waterWalk2 = true;

            if (hideVisual) return;

            player.GetModPlayer<EmbraceOfRainPlayer>().Visible = true;
        }

        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<EmbraceOfRainPlayer>().Visible = true;
        }

        private static bool CanRerollLoot(Player player, NPC npc)
        {
            return player.GetModPlayer<PlayerEffectFlags>().GetFlag<EmbraceOfRainItem>()
                && npc.TryGetGlobalNPC(out NPCTotalDamageByYoyos totalDamageNPC)
                && (totalDamageNPC.TotalDamage > (npc.lifeMax / 4) || totalDamageNPC.TotalDamage < 0);
        }

        private static ItemDropAttemptResult IncreaseLoot(On_ItemDropResolver.orig_ResolveRule orig, ItemDropResolver @this, IItemDropRule rule, DropAttemptInfo info)
        {
            var result = orig(@this, rule, info);

            if (result.State != ItemDropAttemptResultState.FailedRandomRoll
                || isRerolledLoot)
                return result;

            if (info.player is null
                || info.npc is null
                || !CanRerollLoot(info.player, info.npc))
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

    public class EmbraceOfRainPlayer : ModPlayer
    {
        public bool Visible { get; set; }
        public int DyeType { get; private set; }

        public override void ResetEffects()
        {
            Visible = false;
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