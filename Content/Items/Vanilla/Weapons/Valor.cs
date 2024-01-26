using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class ValorItem : VanillaYoyoItem
    {
        public static LocalizedText Tooltip { get; private set; }

        public ValorItem() : base(ItemID.Valor) { }

        public override void Load()
        {
            Tooltip = Language.GetOrRegister("Mods.SPYoyoMod.VanillaItems.ValorItem.Tooltip");
        }

        public override void Unload()
        {
            Tooltip = null;
        }

        public override void SetDefaults(Item item)
        {
            item.knockBack = 6.5f;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            var descriptionLine = new TooltipLine(Mod, "ModTooltip0", Tooltip.Value);
            TooltipUtils.InsertDescription(tooltips, descriptionLine);
        }
    }

    public class ValorProjectile : VanillaYoyoProjectile, IDrawPixelatedPrimitivesProjectile
    {
        public static readonly int ChainChanceDenominator = 7;

        public override bool InstancePerEntity { get => true; }

        private TrailRenderer trailRenderer;
        private SpriteTrailRenderer spriteTrailRenderer;

        public ValorProjectile() : base(ProjectileID.Valor) { }

        public override void AI(Projectile proj)
        {
            if (proj.velocity.Length() >= 3f)
                SpawnDustTrail(proj);

            trailRenderer?.SetNextPoint(proj.Center + proj.velocity);
            spriteTrailRenderer?.SetNextPoint(proj.Center + proj.velocity, proj.rotation);
        }

        public override void OnHitNPC(Projectile proj, NPC npc, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.rand.NextBool(ChainChanceDenominator))
                return;

            if (!npc.CanBeChasedBy(proj, false) || npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type])
                return;

            if (!npc.TryGetGlobalNPC(out ValorGlobalNPC globalNPC))
                return;

            globalNPC.SecureWithChain(npc);
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            if (spriteTrailRenderer is null)
            {
                Main.instance.LoadProjectile(ProjectileID.Valor);

                var texture = TextureAssets.Projectile[ProjectileID.Valor];
                var origin = texture.Size() * 0.5f;

                spriteTrailRenderer = new SpriteTrailRenderer(12, texture, origin, SpriteEffects.None)
                    .SetScale(f => MathHelper.Lerp(1.2f, 0.8f, f))
                    .SetColor(f => Color.Lerp(Color.White, Color.DarkBlue, f) * 0.1f * (1 - f));
            }

            spriteTrailRenderer.Draw(Main.spriteBatch, -Main.screenPosition, lightColor);
            return true;
        }

        private void SpawnDustTrail(Projectile proj)
        {
            var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
            var velocity = vector * Main.rand.NextFloat(0.5f);

            var position = proj.Center;
            position += vector * Main.rand.NextFloat(8f);

            var dust = Dust.NewDustPerfect(position, DustID.DungeonWater, velocity);
            dust.scale += 0.1f;
            dust.noGravity = true;
        }

        void IDrawPixelatedPrimitivesProjectile.PreDrawPixelatedPrimitives(Projectile proj, PrimitiveMatrices matrices)
        {
            trailRenderer ??= new TrailRenderer(12).SetWidth(f => MathHelper.Lerp(24f, 6f, f));

            var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "ValorTrail", AssetRequestMode.ImmediateLoad);
            var effect = effectAsset.Value;
            var effectParameters = effect.Parameters;

            effectParameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Valor_Trail", AssetRequestMode.ImmediateLoad).Value);
            effectParameters["TransformMatrix"].SetValue(matrices.TransformWithScreenOffset);
            effectParameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.025f);

            trailRenderer.Draw(effect);
        }
    }

    public class ValorGlobalNPC : GlobalNPC
    {
        public static readonly float SecuredWithChainMinLength = 16f * 5f;
        public static readonly int SecuredWithChainTime = 60 * 7;

        public override bool InstancePerEntity { get => true; }
        public bool IsSecuredWithChain { get => securedWithChainTimer > 0; }

        private int securedWithChainTimer;
        private float securedWithChainMaxLength;
        private Vector2 chainStartPosition;

        public override void Load()
        {
            On_NPC.UpdateCollision += (orig, npc) =>
            {
                if (npc.TryGetGlobalNPC(out ValorGlobalNPC valorNPC))
                    valorNPC.UpdateCollision(npc);

                orig(npc);
            };

            On_NPC.Teleport += (orig, npc, position, style, extraInfo) =>
            {
                if (npc.TryGetGlobalNPC(out ValorGlobalNPC valorNPC))
                    valorNPC.BreakChain(npc);

                orig(npc, position, style, extraInfo);
            };
        }

        public void SecureWithChain(NPC npc)
        {
            if (IsSecuredWithChain || !TryGetChainStartPoint(npc, out Point point)) return;

            chainStartPosition = point.ToWorldCoordinates();
            securedWithChainTimer = SecuredWithChainTime;
            securedWithChainMaxLength = MathF.Max((npc.Center - chainStartPosition).Length() + 16f, SecuredWithChainMinLength);

            npc.netUpdate = true;

            SpawnDusts(npc.Center);
            SoundEngine.PlaySound(SoundID.Unlock);
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            securedWithChainTimer = -1;
        }

        public override bool PreAI(NPC npc)
        {
            if (!IsSecuredWithChain)
                return true;

            // Goblin Sorcerer, Tim, Dark Caster and others before teleportation
            if (npc.aiStyle == 8 && npc.ai[2] != 0f && npc.ai[3] != 0f)
                BreakChain(npc);

            return true;
        }

        public override void PostAI(NPC npc)
        {
            if (!IsSecuredWithChain)
                return;

            securedWithChainTimer--;

            if (IsSecuredWithChain || (npc.Center - chainStartPosition).Length() > 16f * 25f)
                return;

            BreakChain(npc);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!IsSecuredWithChain)
                return true;

            var npcPosition = (npc.Center + npc.gfxOffY * Vector2.UnitY - Main.screenPosition);
            var chainPosition = chainStartPosition - Main.screenPosition;
            var vectorFromChainToNPC = npcPosition - chainPosition;
            var vectorFromChainToNPCLength = (int)vectorFromChainToNPC.Length();
            var texture = TextureAssets.Chain22;
            var origin = texture.Size() * 0.5f;
            var rotation = vectorFromChainToNPC.ToRotation() + MathHelper.PiOver2;
            var segmentCount = (int)Math.Ceiling((float)vectorFromChainToNPCLength / texture.Width());
            var segmentVector = Vector2.Normalize(vectorFromChainToNPC) * texture.Width();

            for (int i = 0; i < segmentCount; i++)
            {
                var position = chainPosition + segmentVector * i;
                var color = Lighting.GetColor((position + Main.screenPosition).ToTileCoordinates());
                spriteBatch.Draw(texture.Value, position, null, color, rotation, origin, 1f, SpriteEffects.None, 0);
            }

            texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Valor_ChainHead", AssetRequestMode.ImmediateLoad);
            origin = texture.Size() * 0.5f;

            spriteBatch.Draw(texture.Value, chainPosition, null, Lighting.GetColor((chainPosition + Main.screenPosition).ToTileCoordinates()), 0f, origin, 1f, SpriteEffects.None, 0);
            spriteBatch.Draw(texture.Value, npcPosition, null, Lighting.GetColor((npcPosition + Main.screenPosition).ToTileCoordinates()), 0f, origin, 1f, SpriteEffects.None, 0);

            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!IsSecuredWithChain)
                return;

            var npcPosition = (npc.Center + npc.gfxOffY * Vector2.UnitY - Main.screenPosition);
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Valor_Lock", AssetRequestMode.ImmediateLoad);
            var origin = texture.Size() * 0.5f;
            var visualSin = MathF.Sin((float)Main.timeForVisualEffects * 0.03f + npc.whoAmI);

            spriteBatch.Draw(texture.Value, npcPosition, null, Color.White, 0f, origin, 0.9f + visualSin * 0.1f, SpriteEffects.None, 0);
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(securedWithChainTimer);
            binaryWriter.Write(securedWithChainMaxLength);
            binaryWriter.Write(chainStartPosition.X);
            binaryWriter.Write(chainStartPosition.Y);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            securedWithChainTimer = binaryReader.ReadInt32();
            securedWithChainMaxLength = binaryReader.ReadSingle();
            chainStartPosition.X = binaryReader.ReadSingle();
            chainStartPosition.Y = binaryReader.ReadSingle();
        }

        private void BreakChain(NPC npc)
        {
            securedWithChainTimer = -1;
            npc.netUpdate = true;

            SpawnDusts(npc.Center);
            SoundEngine.PlaySound(SoundID.Unlock);
        }

        private void UpdateCollision(NPC npc)
        {
            if (!IsSecuredWithChain)
                return;

            var nextPosition = npc.Center + npc.velocity;
            var vectorFromChainToNPC = nextPosition - chainStartPosition;
            var vectorFromChainToNPCLength = vectorFromChainToNPC.Length();

            if (vectorFromChainToNPCLength > securedWithChainMaxLength)
            {
                var normalizedVectorFromChainToNPC = Vector2.Normalize(vectorFromChainToNPC);
                var newPosition = chainStartPosition + normalizedVectorFromChainToNPC * securedWithChainMaxLength;
                var velocityCorrection = newPosition - nextPosition;

                npc.velocity += velocityCorrection;
            }
        }

        private static void SpawnDusts(Vector2 position)
        {
            for (int i = 0; i < 12; i++)
            {
                var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                var velocity = vector * Main.rand.NextFloat(1f);

                position += vector * Main.rand.NextFloat(8f);

                var dust = Dust.NewDustPerfect(position, DustID.DungeonWater, velocity);
                dust.scale += 0.1f;
                dust.noGravity = true;
            }
        }

        private static bool TryGetChainStartPoint(NPC npc, out Point point)
        {
            var npcTilePos = npc.Bottom.ToTileCoordinates();

            const int X = 9;
            const int Y = 9;

            int x = 0, y = 0, dx = 0;
            int dy = -1;
            int t = Math.Max(X, Y);
            int maxI = t * t;

            for (int i = 0; i < maxI; i++)
            {
                if ((-X / 2 <= x) && (x <= X / 2) && (-Y / 2 <= y) && (y <= Y / 2) && IsRightTile(npcTilePos.X - x, npcTilePos.Y - y))
                {
                    point.X = npcTilePos.X - x;
                    point.Y = npcTilePos.Y - y;
                    return true;
                }

                if ((x == y) || ((x < 0) && (x == -y)) || ((x > 0) && (x == 1 - y)))
                {
                    t = dx;
                    dx = -dy;
                    dy = t;
                }

                x += dx;
                y += dy;
            }

            point = default;
            return false;
        }

        private static bool IsRightTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y) || !Main.tile[x, y].HasTile) return false;
            return WorldGen.SolidTile(x, y) || TileID.Sets.Platforms[Main.tile[x, y].TileType];
        }
    }
}