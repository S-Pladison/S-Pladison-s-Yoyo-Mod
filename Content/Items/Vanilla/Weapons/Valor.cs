using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SPYoyoMod.Common.Graphics.PixelatedLayers;
using SPYoyoMod.Common.Graphics.Renderers;
using SPYoyoMod.Common.Graphics.RenderTargets;
using SPYoyoMod.Common.Networking;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Entities;
using SPYoyoMod.Utils.Rendering;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static Mono.Cecil.Cil.OpCodes;

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
        public const int ChainChanceDenominator = 7;

        public override int YoyoType => ProjectileID.Valor;

        private TrailRenderer trailRenderer;
        private SpriteTrailRenderer spriteTrailRenderer;

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

            Lighting.AddLight(proj.Center, new Color(35, 90, 255).ToVector3() * 0.2f);
        }

        public override void OnHitNPC(Projectile proj, NPC npc, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.rand.NextBool(ChainChanceDenominator))
                return;

            if (!npc.TryGetGlobalNPC(out ValorGlobalNPC globalNPC))
                return;

            if (!globalNPC.TryGetChainStartPoint(npc, out var chainStartPoint))
                return;

            if (!globalNPC.CanSecureWithChain(npc, chainStartPoint))
                return;

            globalNPC.SecureWithChain(npc, chainStartPoint);

            if (proj.owner == Main.myPlayer)
                globalNPC.SendSecureWithChainPacket(npc, chainStartPoint);
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            DrawUtils.DrawGradientYoyoStringWithShadow(proj, mountedCenter, (Color.Transparent, true), (new Color(35, 90, 255), true));
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            trailRenderer ??= new TrailRenderer(12, f => MathHelper.Lerp(24f, 6f, f));

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

        public const float SecuredWithChainLengthMax = 16f * 7f;
        public const float SecuredWithChainLengthMin = 16f * 3f;
        public const int SecuredWithChainTime = 60 * 7;
        public const int CooldownTime = 60 * 3;

        public override bool InstancePerEntity => true;
        public bool IsSecuredWithChain => securedWithChainTimer > 0;
        public bool IsOnCooldown => securedWithChainTimer < 0;

        private int securedWithChainTimer;
        private float securedWithChainLength;
        private Vector2 chainStartPosition;

        public override void Load()
        {
            IL_NPC.UpdateNPC_Inner += (il) =>
            {
                var c = new ILCursor(il);

                c.Index = c.Instrs.Count - 1;

                // if (!noTileCollide)

                // IL_0775: ldarg.0
                // IL_0776: ldfld bool Terraria.NPC::noTileCollide
                // IL_077b: brtrue.s IL_0788

                if (!c.TryGotoPrev(MoveType.Before,
                        i => i.MatchLdarg(0),
                        i => i.MatchLdfld<NPC>("noTileCollide"),
                        i => i.MatchBrtrue(out _))) return;

                c.Index--;

                c.Emit(Ldarg_0);
                c.EmitDelegate<Action<NPC>>(npc =>
                {
                    if (npc.TryGetGlobalNPC(out ValorGlobalNPC valorNPC) && valorNPC.IsSecuredWithChain)
                        valorNPC.UpdateCollision(npc);
                });
            };

            On_NPC.Teleport += (orig, npc, position, style, extraInfo) =>
            {
                if (npc.TryGetGlobalNPC(out ValorGlobalNPC valorNPC))
                    valorNPC.BreakChain(npc);

                orig(npc, position, style, extraInfo);
            };
        }

        public override void OnKill(NPC npc)
        {
            if (!IsSecuredWithChain)
                return;

            BreakChain(npc);
        }

        public bool CanSecureWithChain(NPC npc, Point chainStartPos)
        {
            return !(IsSecuredWithChain
                || IsOnCooldown
                || !npc.CanBeChasedBy()
                || Vector2.Distance(chainStartPos.ToWorldCoordinates(), npc.Center) > SecuredWithChainLengthMax
                || npc.IsBossOrRelated());
        }

        public void SecureWithChain(NPC npc, Point chainStartPos)
        {
            chainStartPosition = chainStartPos.ToWorldCoordinates();
            securedWithChainTimer = SecuredWithChainTime;
            securedWithChainLength = Math.Clamp((chainStartPosition - npc.Center).Length(), SecuredWithChainLengthMin, SecuredWithChainLengthMax);

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
            securedWithChainTimer = -CooldownTime;
            securedWithChainLength = 0;

            npc.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Unlock, npc.Center);
        }

        public void UpdateCollision(NPC npc)
        {
            var nextPosition = npc.Center + npc.velocity;
            var vectorFromChainToNPC = nextPosition - chainStartPosition;
            var vectorFromChainToNPCLength = vectorFromChainToNPC.Length();

            if (vectorFromChainToNPCLength <= securedWithChainLength) return;

            var normalizedVectorFromChainToNPC = Vector2.Normalize(vectorFromChainToNPC);
            var newPosition = chainStartPosition + normalizedVectorFromChainToNPC * securedWithChainLength;
            var velocityCorrection = newPosition - nextPosition;

            npc.velocity += velocityCorrection;
        }

        public override bool PreAI(NPC npc)
        {
            if (!IsSecuredWithChain)
                return true;

            var breakFlag = false;

            // Goblin Sorcerer, Tim, Dark Caster and others before teleportation
            breakFlag |= npc.aiStyle == NPCAIStyleID.Caster && npc.ai[2] != 0f && npc.ai[3] != 0f;

            if (breakFlag)
            {
                BreakChain(npc);
            }

            return true;
        }

        public override void PostAI(NPC npc)
        {
            if (securedWithChainTimer == 0)
                return;

            if (IsOnCooldown)
            {
                securedWithChainTimer++;
                return;
            }
            // else if (IsSecuredWithChain)
            // {

            if (Main.rand.NextBool(5))
            {
                var dust = Main.dust[Dust.NewDust(npc.position, npc.width, npc.height, DustID.DungeonWater, 0f, 0f)];
                dust.velocity *= 0.1f;
                dust.scale += 0.1f;
                dust.noGravity = true;
            }

            securedWithChainTimer--;

            if (IsSecuredWithChain && (npc.Center - chainStartPosition).Length() <= SecuredWithChainLengthMax * 1.2f)
                return;

            BreakChain(npc);

            // }
        }

        public bool TryGetChainStartPoint(NPC npc, out Point point)
        {
            var npcTilePos = npc.Bottom.ToTileCoordinates();

            const int tileCountToCheckX = (int)(SecuredWithChainLengthMax / 16f) * 2 + 1;
            const int tileCountToCheckY = tileCountToCheckX;

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

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            var timerIsNotZero = securedWithChainTimer != 0;

            bitWriter.WriteBit(timerIsNotZero);

            if (timerIsNotZero)
            {
                binaryWriter.Write((short)securedWithChainTimer);
            }
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            var timerIsNotZero = bitReader.ReadBit();

            if (timerIsNotZero)
            {
                securedWithChainTimer = binaryReader.ReadInt16();
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
                DrawUtils.DrawNPC(npc);
            }

            Main.spriteBatch.End();
        }

        public void DrawToScreen()
        {
            if (!IsRenderedInThisFrame || !TryGetRenderTarget(out var target)) return;

            var effect = ModAssets.RequestEffect("ValorNPCOutline").Prepare(parameters =>
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