using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common;
using SPYoyoMod.Common.Graphics.RenderTargets;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Rendering;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public class BowOfDivinePriestessItem : ModItem, IDyeableEquipmentItem
    {
        public class DropCondition : IItemDropRuleCondition, IProvideItemConditionDescription
        {
            public static LocalizedText Description { get; private set; }

            public DropCondition()
            {
                Description ??= Language.GetOrRegister("Mods.SPYoyoMod.DropConditions.BowOfDivinePriestessCondition");
            }

            public bool CanDrop(DropAttemptInfo info)
            {
                return info.npc.value > 0f && !info.IsInSimulation && Main.raining && (info.player.ZoneOverworldHeight || info.player.ZoneSkyHeight);
            }

            public bool CanShowItemDropInUI()
            {
                return true;
            }

            public string GetConditionDescription()
            {
                return Description.Value;
            }
        }

        // Flag to stop recursion of the ItemDropResolver.ResolveRule(...)
        private static bool isRerolledLoot;

        public override string Texture => ModAssets.ItemsPath + "BowOfDivinePriestess";

        public override void Load()
        {
            On_ItemDropResolver.ResolveRule += IncreaseLoot;
        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 46;
            Item.height = 32;

            Item.rare = ItemRarityID.LightPurple;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var modPlayer = player.GetModPlayer<BowOfDivinePriestessPlayer>();

            modPlayer.Equipped = true;
            player.waterWalk2 = true;

            if (hideVisual) return;

            modPlayer.Visible = true;

            ModContent.GetInstance<BowOfDivinePriestessRenderTargetContent>()?.Request();
        }

        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<BowOfDivinePriestessPlayer>().Visible = true;

            ModContent.GetInstance<BowOfDivinePriestessRenderTargetContent>()?.Request();
        }

        void IDyeableEquipmentItem.UpdateDye(Item _, int dye, Player player, bool isNotInVanitySlot, bool isSetToHidden)
        {
            player.GetModPlayer<BowOfDivinePriestessPlayer>().Dye = dye;
        }

        private static bool CanRerollLoot(Player player, NPC npc)
        {
            return player.GetModPlayer<BowOfDivinePriestessPlayer>().Equipped
                && npc.TryGetGlobalNPC(out NPCTotalDamageByYoyos totalDamageNPC)
                && (totalDamageNPC.TotalDamage > (npc.lifeMax / 4));
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

    public class BowOfDivinePriestessPlayer : ModPlayer
    {
        public bool Equipped { get; set; }
        public bool Visible { get; set; }
        public int Dye { get; set; }

        public override void ResetEffects()
        {
            Equipped = false;
            Visible = false;
            Dye = 0;
        }
    }

    public class BowOfDivinePriestessGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return lateInstantiation && item.IsYoyo();
        }

        public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
        {
            if (!player.GetModPlayer<BowOfDivinePriestessPlayer>().Equipped)
                return;

            crit -= 100;
        }
    }

    [Autoload(false)]
    public class BowOfDivinePriestessRenderTargetContent : RenderTargetContent
    {
        private bool anyPlayerWithAcc;

        public override Point Size => new(56, 70);
        //public override Color ClearColor => Color.Black;

        public void Request()
        {
            anyPlayerWithAcc = true;
        }

        public override void Load()
        {
            ModEvents.OnPreUpdatePlayers += () => anyPlayerWithAcc = false;
        }

        public override bool PreRender()
        {
            return anyPlayerWithAcc;
        }

        public override void DrawToTarget()
        {
            var spriteBatchSpanshot = new SpriteBatchSnapshot
            {
                SortMode = SpriteSortMode.Deferred,
                BlendState = BlendState.AlphaBlend,
                SamplerState = Main.DefaultSamplerState,
                DepthStencilState = DepthStencilState.None,
                RasterizerState = RasterizerState.CullNone,
                Effect = null,
                Matrix = Matrix.Identity
            };

            Main.graphics.GraphicsDevice.PrepRenderState(spriteBatchSpanshot);
            Main.spriteBatch.Begin(spriteBatchSpanshot);

            var points = new List<Vector2>();
            var center = Size.ToVector2() / 2f + new Vector2(6, -10);

            points.Add(center + Vector2.UnitY * 14f * new Vector2(0.8f, 1f) + new Vector2(14, 0));

            for (int i = 0; i < 20; i++)
            {
                var angle = MathHelper.TwoPi / 20f * i;
                var offset = Vector2.UnitY.RotatedBy(angle) * 14f * new Vector2(0.8f, 1f);

                var position = center + offset;
                position.X += offset.Y * 0.7f;

                points.Add(position);
            }

            var effect = ModAssets.RequestEffect("Test").Prepare(parameters =>
            {
                parameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.MiscPath + "Test").Value);
                parameters["TransformMatrix"].SetValue(Matrix.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, -1, 1));
                parameters["Time"].SetValue((float)Main.timeForVisualEffects * 0.02f);
            });

            DrawUtils.DrawPrimitiveStrip(effect.Value, points, _ => 22f, false);

            //Main.spriteBatch.Draw(TextureAssets.Sun.Value, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 0.3f, SpriteEffects.None, 0);

            Main.spriteBatch.End();
        }
    }

    [Autoload(false)]
    public class BowOfDivinePriestessPlayerLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new Between(PlayerDrawLayers.JimsCloak, PlayerDrawLayers.MountBack);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            var player = drawInfo.drawPlayer;

            return !player.dead && !player.invis && player.GetModPlayer<BowOfDivinePriestessPlayer>().Visible;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            var rtContent = ModContent.GetInstance<BowOfDivinePriestessRenderTargetContent>();

            if (!rtContent.WasRenderedInThisFrame || !rtContent.TryGetRenderTarget(out var embraceOfRainTarget))
                return;

            var player = drawInfo.drawPlayer;
            var position = (player.MountedCenter + new Vector2(-60 * player.direction, -12 * player.gravDir + player.gfxOffY) - Main.screenPosition).Floor();
            var color = Color.White * player.stealth;
            var origin = embraceOfRainTarget.Size() * 0.5f;
            var spriteEffects = SpriteEffects.None;

            if (player.direction < 0)
                spriteEffects |= SpriteEffects.FlipHorizontally;

            if (player.gravDir < 0)
                spriteEffects |= SpriteEffects.FlipVertically;

            drawInfo.DrawDataCache.Add(
                new DrawData(embraceOfRainTarget, position, null, color, 0f, origin, 1f, spriteEffects, 0) with
                {
                    shader = player.GetModPlayer<BowOfDivinePriestessPlayer>().Dye
                }
            );
        }
    }
}