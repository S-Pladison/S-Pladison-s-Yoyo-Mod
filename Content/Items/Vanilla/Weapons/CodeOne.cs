using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.PixelatedLayers;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using SPYoyoMod.Utils.Extensions;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class CodeOneItem : VanillaYoyoItem
    {
        public CodeOneItem() : base(ItemID.Code1) { }

        public override void Load()
        {
            On_Player.GetWeaponDamage += (orig, player, item, forTooltip) =>
            {
                if (!forTooltip)
                    return orig(player, item, forTooltip);

                var isCode1 = item.type == ItemID.Code1;

                if (isCode1 && player.GetModPlayer<CodeOnePlayer>().IsBuffActive)
                    ItemID.Sets.ToolTipDamageMultiplier[item.type] = 1.33f;

                var result = orig(player, item, forTooltip);

                if (isCode1)
                    ItemID.Sets.ToolTipDamageMultiplier[item.type] = 1f;

                return result;
            };
        }
    }

    public class CodeOneProjectile : VanillaYoyoProjectile
    {
        private bool buffActive;

        public CodeOneProjectile() : base(ProjectileID.Code1) { }

        public override void AI(Projectile proj)
        {
            if (Main.myPlayer == proj.owner)
            {
                var oldBuffActiveValue = buffActive;
                buffActive = Main.player[proj.owner].GetModPlayer<CodeOnePlayer>().IsBuffActive;

                if (oldBuffActiveValue != buffActive)
                    proj.netUpdate = true;
            }

            proj.localAI[1] = MathHelper.Clamp(proj.localAI[1] + (buffActive ? 0.05f : -0.02f), 0f, 1f);
        }

        public override void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!buffActive) return;

            modifiers.FinalDamage += 0.33f;
        }

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer == proj.owner)
            {
                var codeOnePlayer = Main.player[proj.owner].GetModPlayer<CodeOnePlayer>();
                codeOnePlayer.AddTimerToDict(target);
            }

            if (!buffActive) return;

            Projectile.NewProjectile(proj.GetSource_FromThis(), proj.Center, Vector2.Zero, ModContent.ProjectileType<CodeOneHitProjectile>(), 0, 0, proj.owner);
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(buffActive);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            buffActive = bitReader.ReadBit();
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            if (proj.localAI[1] <= 0f) return;

            var progress = EasingFunctions.InOutQuint(proj.localAI[1]);

            DrawUtils.DrawYoyoString(proj, mountedCenter, (segmentCount, segmentIndex, position, rotation, height, color) =>
            {
                var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/FishingLine_WithShadow", AssetRequestMode.ImmediateLoad);
                var pos = position - Main.screenPosition;
                var rect = new Rectangle(0, 0, texture.Width(), (int)height);
                var origin = new Vector2(texture.Width() * 0.5f, 0f);
                var colour = Color.Lerp(Color.Transparent, new Color(65, 185, 255) * progress, EasingFunctions.InQuart(segmentIndex / (float)segmentCount) * 5f);

                Main.spriteBatch.Draw(texture.Value, pos, rect, colour, rotation, origin, 1f, SpriteEffects.None, 0f);
            });
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            if (proj.localAI[1] <= 0f) return true;

            var progress = EasingFunctions.InOutQuint(proj.localAI[1]);
            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Yoyo_GlowWithShadow", AssetRequestMode.ImmediateLoad);
            var color = new Color(65, 185, 255) * progress;

            Main.spriteBatch.Draw(texture.Value, position, null, color, proj.rotation, texture.Size() * 0.5f, proj.scale * 1.2f, SpriteEffects.None, 0f);
            return true;
        }
    }

    // Warning: Client-Side only
    public class CodeOnePlayer : ModPlayer
    {
        public const int TimeToForgetNPC = 60 * 5;

        private class TimerData
        {
            public int NpcType;
            public int Counter;

            public TimerData(int npcType)
            {
                NpcType = npcType;
                Counter = TimeToForgetNPC;
            }
        }

        private readonly Dictionary<int, TimerData> timers;

        public CodeOnePlayer()
        {
            timers = new Dictionary<int, TimerData>();
        }

        public void AddTimerToDict(NPC npc)
        {
            timers[npc.whoAmI] = new TimerData(npc.type);
        }

        public override void PostUpdate()
        {
            foreach (var npcWhoAmI in timers.Keys)
            {
                var timerData = timers[npcWhoAmI];
                timerData.Counter--;

                ref var npc = ref Main.npc[npcWhoAmI];

                if (!npc.active || npc.type != timerData.NpcType || timerData.Counter < 0)
                    timers.Remove(npcWhoAmI);
            }
        }

        public bool IsBuffActive => timers.Count <= 2;
    }

    public class CodeOneHitProjectile : ModProjectile
    {
        public const float InitTimeLeft = 15f;

        public override string Texture { get => ModAssets.TexturesPath + "Effects/Invisible"; }

        private RingRenderer ringRenderer;

        public override void SetDefaults()
        {
            Projectile.DefaultToVisualEffect();

            Projectile.timeLeft = (int)InitTimeLeft;
        }

        public override void OnKill(int timeLeft)
        {
            ringRenderer?.Dispose();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            ringRenderer ??= new RingRenderer(26, 16f, 16f);

            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.UnderProjectiles, () =>
            {
                var factor = Projectile.timeLeft / InitTimeLeft;
                var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
                var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/CodeOneHit_Ring", AssetRequestMode.ImmediateLoad);

                var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "DefaultStrip", AssetRequestMode.ImmediateLoad);
                var effect = effectAsset.Value;
                var effectParameters = effect.Parameters;

                effectParameters["Texture0"].SetValue(texture.Value);
                effectParameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.Transform);

                var colorProgress = EasingFunctions.OutQuint(factor);
                var color = (Color.Lerp(new Color(55, 0, 255), new Color(65, 185, 255), colorProgress) * colorProgress) with { A = 0 };
                var colorVec4 = color.ToVector4();

                effectParameters["ColorTL"].SetValue(colorVec4);
                effectParameters["ColorTR"].SetValue(colorVec4);
                effectParameters["ColorBL"].SetValue(colorVec4);
                effectParameters["ColorBR"].SetValue(colorVec4);

                ringRenderer?
                    .SetRadius(16f * 1.275f * EasingFunctions.OutCubic(1f - factor))
                    .SetPosition(position)
                    .Draw(effect);
            });

            var factor = Projectile.timeLeft / InitTimeLeft;
            var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var colorProgress = EasingFunctions.OutSine(factor);

            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Circle_BlackToAlpha_PremultipliedAlpha", AssetRequestMode.ImmediateLoad);
            var color = Color.Black * colorProgress * 0.7f;
            var rotation = 0f;
            var scale = EasingFunctions.OutCubic(1f - factor) * 0.35f;

            Main.spriteBatch.Draw(texture.Value, position, null, color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Circle", AssetRequestMode.ImmediateLoad);
            color = (Color.Lerp(new Color(55, 0, 255, 0), new Color(140, 210, 255, 255), colorProgress) * colorProgress * 0.65f) with { A = 0 };
            rotation = 0f;
            scale = EasingFunctions.OutCubic(1f - factor) * 0.25f;

            Main.spriteBatch.Draw(texture.Value, position, null, color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/CodeOneHit_Rainbow", AssetRequestMode.ImmediateLoad);
            color = (Color.White * (colorProgress - 0.25f)) with { A = 0 };
            rotation = (factor + ((int)Projectile.position.X ^ (int)Projectile.position.Y)) * 0.3f;
            scale = EasingFunctions.OutCubic(1f - factor) * 0.2f;

            Main.spriteBatch.Draw(texture.Value, position, null, color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            return true;
        }
    }
}