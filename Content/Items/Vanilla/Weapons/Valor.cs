using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.Networking;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using SPYoyoMod.Utils.Extensions;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class ValorItem : VanillaYoyoItem
    {
        public ValorItem() : base(ItemID.Valor) { }

        public override void SetDefaults(Item item)
        {
            item.knockBack = 6.5f;
        }
    }

    public class ValorProjectile : VanillaYoyoProjectile, IPreDrawPixelatedProjectile
    {
        public static readonly int ChainChanceDenominator = 7;

        private TrailRenderer trailRenderer;
        private SpriteTrailRenderer spriteTrailRenderer;

        public ValorProjectile() : base(ProjectileID.Valor) { }

        public override void OnKill(Projectile proj, int timeLeft)
        {
            trailRenderer?.Dispose();
        }

        public override void AI(Projectile proj)
        {
            if (proj.velocity.Length() >= 3f)
            {
                var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                var velocity = vector * Main.rand.NextFloat(0.5f);

                var position = proj.Center;
                position += vector * Main.rand.NextFloat(8f);

                var dust = Dust.NewDustPerfect(position, DustID.DungeonWater, velocity);
                dust.scale += 0.1f;
                dust.noGravity = true;
            }

            trailRenderer?.SetNextPoint(proj.Center + proj.velocity);
            spriteTrailRenderer?.SetNextPoint(proj.Center + proj.velocity, proj.rotation);
        }

        public override void OnHitNPC(Projectile proj, NPC npc, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.rand.NextBool(ChainChanceDenominator))
                return;

            if (!npc.CanBeChasedBy(proj, false))
                return;

            if (!npc.TryGetGlobalNPC(out ValorGlobalNPC globalNPC))
                return;

            if (!globalNPC.TryGetChainStartPoint(npc, out Point chainStartPoint))
                return;

            globalNPC.SecureWithChain(npc, chainStartPoint);

            if (proj.owner == Main.myPlayer)
                globalNPC.SendSecureWithChainPacket(npc, chainStartPoint);
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            spriteTrailRenderer ??= InitSpriteTrail();
            spriteTrailRenderer.Draw(Main.spriteBatch, -Main.screenPosition, lightColor);
            return true;
        }

        public SpriteTrailRenderer InitSpriteTrail()
        {
            Main.instance.LoadProjectile(ProjectileID.Valor);

            var texture = TextureAssets.Projectile[ProjectileID.Valor];
            var origin = texture.Size() * 0.5f;

            spriteTrailRenderer = new SpriteTrailRenderer(12, texture, origin, SpriteEffects.None)
                .SetScale(f => MathHelper.Lerp(1.2f, 0.8f, f))
                .SetColor(f => Color.Lerp(Color.White, Color.DarkBlue, f) * 0.1f * (1 - f));

            return spriteTrailRenderer;
        }

        void IPreDrawPixelatedProjectile.PreDrawPixelated(Projectile proj)
        {
            trailRenderer ??= new TrailRenderer(12).SetWidth(f => MathHelper.Lerp(24f, 6f, f));

            var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "ValorTrail", AssetRequestMode.ImmediateLoad);
            var effect = effectAsset.Value;
            var effectParameters = effect.Parameters;

            effectParameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Valor_Trail", AssetRequestMode.ImmediateLoad).Value);
            effectParameters["TransformMatrix"].SetValue(ProjectileDrawLayers.PixelatedPrimitiveMatrices.TransformWithScreenOffset);
            effectParameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.025f);

            trailRenderer.Draw(effect);
        }
    }

    public class ValorGlobalNPC : GlobalNPC
    {
        private class SecureWithChainPacket : NetPacket
        {
            public readonly byte NPCWhoAmI;
            public readonly short NPCType;
            public readonly short ChainStartPosX;
            public readonly short ChainStartPosY;

            public SecureWithChainPacket() { }

            public SecureWithChainPacket(NPC npc, Point chainStartPos) : this(npc.whoAmI, npc.type, chainStartPos) { }

            public SecureWithChainPacket(int npcWhoAmI, int npcType, Point chainStartPos)
            {
                NPCWhoAmI = (byte)npcWhoAmI;
                NPCType = (short)npcType;
                ChainStartPosX = (short)chainStartPos.X;
                ChainStartPosY = (short)chainStartPos.Y;
            }

            public override void Send(BinaryWriter writer)
            {
                writer.Write(NPCType);
                writer.Write(NPCWhoAmI);
                writer.Write(ChainStartPosX);
                writer.Write(ChainStartPosY);
            }

            public override void Receive(BinaryReader reader, int sender)
            {
                var chainStartPos = Point.Zero;

                var npcType = reader.ReadInt16();
                var npcWhoAmI = reader.ReadByte();
                chainStartPos.X = (int)reader.ReadInt16();
                chainStartPos.Y = (int)reader.ReadInt16();

                var npc = Main.npc[npcWhoAmI];

                if (npc.type != npcType) return;
                if (!npc.TryGetGlobalNPC(out ValorGlobalNPC globalNPC)) return;

                globalNPC.SecureWithChain(npc, chainStartPos);

                if (Main.netMode == NetmodeID.Server)
                {
                    new SecureWithChainPacket(npcWhoAmI, npcType, chainStartPos).Send(-1, sender);
                }
            }
        }

        public static readonly float SecuredWithChainLengthLimit = 16f * 5f;
        public static readonly int SecuredWithChainTime = 60 * 7;

        public override bool InstancePerEntity { get => true; }
        public bool IsSecuredWithChain { get => securedWithChainTimer > 0; }

        private int securedWithChainTimer;
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

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (!IsSecuredWithChain) return;

            DrawChainSegments(npc, Main.spriteBatch);
            DrawChainHeadAndTail(npc, Main.spriteBatch);
        }

        public void DrawChainSegments(NPC npc, SpriteBatch spriteBatch)
        {
            var texture = TextureAssets.Chain22;
            var endPosition = (npc.Center + npc.gfxOffY * Vector2.UnitY - Main.screenPosition);
            var startPosition = chainStartPosition - Main.screenPosition;
            var vectorFromChainToNPC = endPosition - startPosition;
            var vectorFromChainToNPCLength = (int)vectorFromChainToNPC.Length();

            var segmentRotation = vectorFromChainToNPC.ToRotation() + MathHelper.PiOver2;
            var segmentOrigin = texture.Size() * 0.5f;
            var segmentCount = (int)Math.Ceiling((float)vectorFromChainToNPCLength / texture.Width());
            var segmentVector = Vector2.Normalize(vectorFromChainToNPC) * texture.Width();

            for (int i = 0; i < segmentCount; i++)
            {
                var position = startPosition + segmentVector * i;
                var color = Lighting.GetColor((position + Main.screenPosition).ToTileCoordinates());
                spriteBatch.Draw(texture.Value, position, null, color, segmentRotation, segmentOrigin, 1f, SpriteEffects.None, 0);
            }
        }

        public void DrawChainHeadAndTail(NPC npc, SpriteBatch spriteBatch)
        {
            var endPosition = (npc.Center + npc.gfxOffY * Vector2.UnitY - Main.screenPosition);
            var startPosition = chainStartPosition - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Valor_ChainHead", AssetRequestMode.ImmediateLoad);
            var origin = texture.Size() * 0.5f;

            var color = Lighting.GetColor((startPosition + Main.screenPosition).ToTileCoordinates());

            spriteBatch.Draw(texture.Value, startPosition, null, color, 0f, origin, 1f, SpriteEffects.None, 0);

            color = Lighting.GetColor((endPosition + Main.screenPosition).ToTileCoordinates());

            spriteBatch.Draw(texture.Value, endPosition, null, color, 0f, origin, 1f, SpriteEffects.None, 0);
        }

        public void SecureWithChain(NPC npc, Point chainStartPos)
        {
            if (IsSecuredWithChain
                || npc.noTileCollide
                || npc.boss
                || NPCID.Sets.ShouldBeCountedAsBoss[npc.type]) return;

            chainStartPosition = chainStartPos.ToWorldCoordinates();
            securedWithChainTimer = SecuredWithChainTime;

            npc.netUpdate = true;

            SpawnEffectDusts(npc.Center);
            SoundEngine.PlaySound(SoundID.Unlock, npc.Center);
        }

        public void SendSecureWithChainPacket(NPC npc, Point chainStartPos)
            => new SecureWithChainPacket(npc, chainStartPos).Send();

        public void BreakChain(NPC npc)
        {
            securedWithChainTimer = -1;
            npc.netUpdate = true;

            SpawnEffectDusts(npc.Center);
            SoundEngine.PlaySound(SoundID.Unlock, npc.Center);
        }

        public void UpdateCollision(NPC npc)
        {
            if (!IsSecuredWithChain)
                return;

            var nextPosition = npc.Center + npc.velocity;
            var vectorFromChainToNPC = nextPosition - chainStartPosition;
            var vectorFromChainToNPCLength = vectorFromChainToNPC.Length();

            if (vectorFromChainToNPCLength > SecuredWithChainLengthLimit)
            {
                var normalizedVectorFromChainToNPC = Vector2.Normalize(vectorFromChainToNPC);
                var newPosition = chainStartPosition + normalizedVectorFromChainToNPC * SecuredWithChainLengthLimit;
                var velocityCorrection = newPosition - nextPosition;

                npc.velocity += velocityCorrection;
            }
        }

        public void SpawnEffectDusts(Vector2 position)
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

        public bool TryGetChainStartPoint(NPC npc, out Point point)
        {
            var npcTilePos = npc.Bottom.ToTileCoordinates();

            const int tileCountToCheckX = 9;
            const int tileCountToCheckY = 9;

            const int halfTileCountToCheckX = tileCountToCheckX / 2;
            const int halfTileCountToCheckY = tileCountToCheckY / 2;

            int x = 0, y = 0, dx = 0;
            int dy = -1;
            int t = Math.Max(tileCountToCheckX, tileCountToCheckY);
            int maxI = t * t;

            for (int i = 0; i < maxI; i++)
            {
                if (-halfTileCountToCheckX <= x
                    && x <= halfTileCountToCheckX
                    && -halfTileCountToCheckY <= y
                    && y <= halfTileCountToCheckY
                    && IsRightTile(npcTilePos.X - x, npcTilePos.Y - y))
                {
                    point.X = npcTilePos.X - x;
                    point.Y = npcTilePos.Y - y;
                    return true;
                }

                if (x == y
                    || (x < 0 && x == -y)
                    || (x > 0 && x == 1 - y))
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
            return WorldGen.SolidOrSlopedTile(x, y) || Main.tile[x, y].IsHalfBlock || TileID.Sets.Platforms[Main.tile[x, y].TileType];
        }
    }

    public class ValorRenderTargetContent : EntityRenderTargetContent<NPC>
    {
        public override void Load()
        {
            On_Main.DoDraw_Tiles_NonSolid += (orig, main) =>
            {
                orig(main);

                if (!IsRenderedInThisFrame || !TryGetRenderTarget(out RenderTarget2D target)) return;

                var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "ValorEffect", AssetRequestMode.ImmediateLoad);
                var effect = effectAsset.Value;
                var effectParameters = effect.Parameters;

                effectParameters["ScreenSize"].SetValue(target.Size());
                effectParameters["OutlineColor"].SetValue(new Color(44, 66, 255, 170).ToVector4());
                effectParameters["Zoom"].SetValue(new Vector2(Main.GameZoomTarget));

                Main.spriteBatch.End(out SpriteBatchSnapshot spriteBatchSnapshot);
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, effect, Matrix.Identity);
                Main.spriteBatch.Draw(target, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(spriteBatchSnapshot);
            };
        }

        public override bool CanDrawEntity(NPC npc)
            => npc.GetGlobalNPC<ValorGlobalNPC>().IsSecuredWithChain;

        public override void DrawEntity(NPC npc)
            => DrawUtils.DrawNPC(npc, false);

        public override void DrawToTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            DrawEntities();
            Main.spriteBatch.End();
        }
    }
}