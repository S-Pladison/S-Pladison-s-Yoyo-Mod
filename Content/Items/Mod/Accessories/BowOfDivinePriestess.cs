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
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public class BowOfDivinePriestessItem : ModItem, IDyeableEquipmentItem
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

    public class BowOfDivinePriestessRenderTargetContent : RenderTargetContent
    {
        private bool anyPlayerWithAcc;

        public override Point Size => new(56, 70);
        public override Color ClearColor => Color.Black;

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

            for (int i = 0; i < 20; i++)
            {
                var angle = MathHelper.TwoPi / 20f * i;
                points.Add(center + Vector2.UnitX.RotatedBy(angle) * 14f * new Vector2(0.8f, 1f));
            }

            var effect = ModAssets.RequestEffect("DefaultStrip").Prepare(parameters =>
            {
                parameters["Texture0"].SetValue(TextureAssets.MagicPixel.Value);
                parameters["TransformMatrix"].SetValue(Matrix.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, -1, 1));

                var colorVec4 = (Color.White).ToVector4();

                parameters["ColorTL"].SetValue(colorVec4);
                parameters["ColorTR"].SetValue(colorVec4);
                parameters["ColorBL"].SetValue(colorVec4);
                parameters["ColorBR"].SetValue(colorVec4);
            });

            DrawUtils.DrawPrimitiveStrip(effect.Value, points, _ => 5f, false);

            //Main.spriteBatch.Draw(TextureAssets.Sun.Value, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 0.3f, SpriteEffects.None, 0);

            Main.spriteBatch.End();
        }
    }

    public class BowOfDivinePriestessPlayerLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new Between(PlayerDrawLayers.JimsCloak, PlayerDrawLayers.MountBack);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            var player = drawInfo.drawPlayer;

            return !player.dead && player.GetModPlayer<BowOfDivinePriestessPlayer>().Visible;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            var rtContent = ModContent.GetInstance<BowOfDivinePriestessRenderTargetContent>();

            if (!rtContent.WasRenderedInThisFrame || !rtContent.TryGetRenderTarget(out var embraceOfRainTarget))
                return;

            var player = drawInfo.drawPlayer;
            var position = (player.MountedCenter + new Vector2(-16 * player.direction, -12 * player.gravDir + player.gfxOffY) - Main.screenPosition).Floor();
            var spriteEffects = SpriteEffects.None;

            if (player.direction < 0)
                spriteEffects |= SpriteEffects.FlipHorizontally;

            if (player.gravDir < 0)
                spriteEffects |= SpriteEffects.FlipVertically;

            drawInfo.DrawDataCache.Add(
                new DrawData(embraceOfRainTarget, position, null, Color.White, 0f, embraceOfRainTarget.Size() * 0.5f, 1f, spriteEffects, 0) with
                {
                    shader = player.GetModPlayer<BowOfDivinePriestessPlayer>().Dye
                }
            );
        }
    }
}