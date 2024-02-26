using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Networking;
using SPYoyoMod.Common.PixelatedLayers;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Rendering;
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
        public override int YoyoType => ItemID.Valor;

        public override void SetDefaults(Item item)
        {
            item.knockBack = 6.5f;
        }
    }

    public class ValorProjectile : VanillaYoyoProjectile
    {
        public static readonly int ChainChanceDenominator = 7;

        public override int YoyoType => ProjectileID.Valor;

        private TrailRenderer trailRenderer;
        private SpriteTrailRenderer spriteTrailRenderer;

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

            if (!npc.TryGetGlobalNPC(out ValorGlobalNPC globalNPC))
                return;

            if (!globalNPC.TryGetChainStartPoint(npc, out var chainStartPoint))
                return;

            globalNPC.SecureWithChain(npc, chainStartPoint);

            if (proj.owner == Main.myPlayer)
                globalNPC.SendSecureWithChainPacket(npc, chainStartPoint);
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            DrawUtils.DrawYoyoString(proj, mountedCenter, (segmentCount, segmentIndex, position, rotation, height, color) =>
            {
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "FishingLine_WithShadow", AssetRequestMode.ImmediateLoad);
                var pos = position - Main.screenPosition;
                var rect = new Rectangle(0, 0, texture.Width(), (int)height);
                var origin = new Vector2(texture.Width() * 0.5f, 0f);
                var colour = Color.Lerp(Color.Transparent, new Color(35, 90, 255), EasingFunctions.InQuart(segmentIndex / (float)segmentCount) * 5f);

                Main.spriteBatch.Draw(texture.Value, pos, rect, colour, rotation, origin, 1f, SpriteEffects.None, 0f);
            });
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            trailRenderer ??= new TrailRenderer(12).SetWidth(f => MathHelper.Lerp(24f, 6f, f));

            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.UnderProjectiles, () =>
            {
                trailRenderer?.Draw(ModAssets.RequestEffect("ValorTrail").Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.MiscPath + "Valor_Trail", AssetRequestMode.ImmediateLoad).Value);
                    parameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.TransformWithScreenOffset);
                    parameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.025f);
                }));
            });

            spriteTrailRenderer ??= InitSpriteTrail();
            spriteTrailRenderer.Draw(Main.spriteBatch, -Main.screenPosition, lightColor);

            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Yoyo_GlowWithShadow", AssetRequestMode.ImmediateLoad);

            Main.spriteBatch.Draw(texture.Value, position, null, new Color(35, 90, 255), proj.rotation, texture.Size() * 0.5f, proj.scale * 1.2f, SpriteEffects.None, 0f);

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

        public const float SecuredWithChainLengthLimit = 16f * 5f;
        public const int SecuredWithChainTime = 60 * 7;

        public override bool InstancePerEntity => true;
        public bool IsSecuredWithChain => securedWithChainTimer > 0;

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

            if (Main.rand.NextBool(5))
            {
                var dust = Main.dust[Dust.NewDust(npc.position, npc.width, npc.height, DustID.DungeonWater, 0f, 0f)];
                dust.velocity *= 0.1f;
                dust.scale += 0.1f;
                dust.noGravity = true;
            }

            securedWithChainTimer--;

            if (IsSecuredWithChain || (npc.Center - chainStartPosition).Length() > 16f * 25f)
                return;

            BreakChain(npc);
        }

        public override void OnKill(NPC npc)
        {
            if (!IsSecuredWithChain)
                return;

            BreakChain(npc);
        }

        public void SecureWithChain(NPC npc, Point chainStartPos)
        {
            if (IsSecuredWithChain
                || !npc.CanBeChasedBy()
                || npc.noTileCollide
                || npc.boss
                || NPCID.Sets.ShouldBeCountedAsBoss[npc.type]) return;

            chainStartPosition = chainStartPos.ToWorldCoordinates();
            securedWithChainTimer = SecuredWithChainTime;

            npc.netUpdate = true;

            ModContent.GetInstance<ValorRenderTargetContent>()?.AddNPC(npc);
            SoundEngine.PlaySound(SoundID.Unlock, npc.Center);
        }

        public void SendSecureWithChainPacket(NPC npc, Point chainStartPos)
        {
            new SecureWithChainPacket(npc, chainStartPos).Send();
        }

        public void BreakChain(NPC npc)
        {
            securedWithChainTimer = -1;
            npc.netUpdate = true;

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

            for (var i = 0; i < segmentCount; i++)
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
            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Valor_ChainHead", AssetRequestMode.ImmediateLoad);
            var origin = texture.Size() * 0.5f;

            var color = Lighting.GetColor((startPosition + Main.screenPosition).ToTileCoordinates());

            spriteBatch.Draw(texture.Value, startPosition, null, color, 0f, origin, 1f, SpriteEffects.None, 0);

            color = Lighting.GetColor((endPosition + Main.screenPosition).ToTileCoordinates());

            spriteBatch.Draw(texture.Value, endPosition, null, color, 0f, origin, 1f, SpriteEffects.None, 0);
        }

        public bool TryGetChainStartPoint(NPC npc, out Point point)
        {
            var npcTilePos = npc.Bottom.ToTileCoordinates();

            const int tileCountToCheckX = 9;
            const int tileCountToCheckY = 9;

            const int halfTileCountToCheckX = tileCountToCheckX / 2;
            const int halfTileCountToCheckY = tileCountToCheckY / 2;

            int x = 0, y = 0, dx = 0;
            var dy = -1;
            var t = Math.Max(tileCountToCheckX, tileCountToCheckY);
            var maxI = t * t;

            for (var i = 0; i < maxI; i++)
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

    public class ValorRenderTargetContent : RenderTargetContent
    {
        public override Point Size => new(Main.screenWidth, Main.screenHeight);

        private NPCObserver npcObserver;

        public override void Load()
        {
            npcObserver = new(n => !n.TryGetGlobalNPC(out ValorGlobalNPC valorNPC) || !valorNPC.IsSecuredWithChain);

            ModEvents.OnPostUpdateEverything += npcObserver.Update;
            ModEvents.OnWorldUnload += npcObserver.Clear;

            On_Main.DoDraw_Tiles_NonSolid += (orig, main) =>
            {
                orig(main);
                DrawToScreen();
            };
        }

        public void AddNPC(NPC npc)
        {
            npcObserver.Add(npc);
        }

        public override bool PreRender()
        {
            return npcObserver.AnyEntity;
        }

        public override void DrawToTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (var npc in npcObserver.GetEntityInstances())
            {
                DrawUtils.DrawNPC(npc, false);
            }

            Main.spriteBatch.End();
        }

        public void DrawToScreen()
        {
            if (!IsRenderedInThisFrame || !TryGetRenderTarget(out var target)) return;

            var effect = ModAssets.RequestEffect("ValorEffect").Prepare(parameters =>
            {
                parameters["ScreenSize"].SetValue(target.Size());
                parameters["OutlineColor"].SetValue(new Color(35, 90, 255).ToVector4());
                parameters["Zoom"].SetValue(new Vector2(Main.GameZoomTarget));
            });

            Main.spriteBatch.End(out var spriteBatchSnapshot);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, effect.Value, Matrix.Identity);
            Main.spriteBatch.Draw(target, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(spriteBatchSnapshot);
        }
    }
}