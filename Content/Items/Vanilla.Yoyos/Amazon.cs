using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Common.Yoyos;
using SPYoyoMod.Core.Netcode;
using SPYoyoMod.Utils;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class AmazonItem : YoyoItem<AmazonProjectile>
    {
        public override int OverrideType => ItemID.JungleYoyo;
    }

    public sealed class AmazonProjectile : YoyoProjectile<AmazonItem>
    {
        public override int OverrideType => ProjectileID.JungleYoyo;

        //=/-

        public static readonly int AttachChanceDenominator = 7;
        public static readonly int AttachMinRemainingHits = 3;
        public static readonly int AttachDuration = GeneralUtils.SecondsToTicks(7f);
        public static readonly int AttachSearchRadius = TileUtils.TileSizeInPixels * 7;
        public static readonly float AttachExtraLength = TileUtils.TileSizeInPixels * 2.5f;
        public static readonly float AttachBreakDistance = TileUtils.TileSizeInPixels * 12f;
        public static readonly float AttachChanceReductionDistance = TileUtils.TileSizeInPixels * 12f;

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!target.TryGetGlobalNPC(out AmazonGlobalNPC amazonNPC))
                return;

            amazonNPC.TryAnchorFromHit(target, damageDone);
        }
    }

    public sealed class AmazonGlobalNPC : GlobalNPC
    {
        public sealed class AnchorData(Point tile, float length, int timeLeft)
        {
            public Point Tile = tile;
            public float Length = length;
            public int TimeLeft = timeLeft;

            public Vector2 WorldPosition => Tile.ToWorldCoordinates();
        }

        private sealed class AmazonAnchorPacket : NetPacket
        {
            public override void Send(BinaryWriter writer, params object[] context)
            {
                writer.Write((byte)context[0]); //< npcWhoAmI
                writer.Write((ushort)context[1]); //< npcType

                var data = context.Length > 2 ? context[2] as AnchorData : null;

                writer.Write(data is not null);

                if (data is null)
                    return;

                writer.Write((ushort)data.Tile.X);
                writer.Write((ushort)data.Tile.Y);
                writer.Write(data.Length);
                writer.Write7BitEncodedInt(data.TimeLeft);
            }

            public override void Receive(BinaryReader reader, int sender)
            {
                var npcWhoAmI = reader.ReadByte();
                var npcType = reader.ReadUInt16();
                var hasData = reader.ReadBoolean();
                var npc = Main.npc[npcWhoAmI];

                if (npc is null || npc.type != npcType || !npc.TryGetGlobalNPC(out AmazonGlobalNPC amazonNPC))
                    return;

                if (Main.netMode == NetmodeID.Server)
                {
                    if (npc.active)
                        amazonNPC.TryAnchor(npc);

                    return;
                }

                if (!hasData)
                {
                    amazonNPC.SetAnchorData(npc, null);
                    return;
                }

                var tile = new Point(reader.ReadUInt16(), reader.ReadUInt16());
                var length = reader.ReadSingle();
                var timeLeft = reader.Read7BitEncodedInt();

                amazonNPC.SetAnchorData(npc, new AnchorData(tile, length, timeLeft));
            }
        }

        public override bool InstancePerEntity => true;
        public AnchorData Data { get; private set; }

        public override void Load()
        {
            // Коллизия, ограничивающая перемещение НПС в определенном радиусе
            // Из-за мелких артефактов по типу тряски и т.п., решил что лучше решения не будет
            IL_NPC.UpdateNPC_Inner += il =>
            {
                var cursor = new ILCursor(il);

                // Идем в конец функции
                cursor.Index = cursor.Instrs.Count - 1;

                // if (!noTileCollide)

                // IL_0775: ldarg.0
                // IL_0776: ldfld bool Terraria.NPC::noTileCollide
                // IL_077b: brtrue.s IL_0788

                if (!cursor.TryGotoPrev(
                    MoveType.Before,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<NPC>("noTileCollide"),
                    i => i.MatchBrtrue(out _)))
                {
                    ModLogger.Warn($"IL edit \"{nameof(AmazonGlobalNPC)}..{nameof(IL_NPC.UpdateNPC_Inner)}\" failed...");
                    return;
                }

                cursor.Index--;

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<NPC>>(npc =>
                {
                    if (npc.TryGetGlobalNPC(out AmazonGlobalNPC amazonNPC))
                        amazonNPC.Constrain(npc);
                });
            };

            On_NPC.Teleport += (orig, npc, position, style, extraInfo) =>
            {
                if (npc.TryGetGlobalNPC(out AmazonGlobalNPC amazonNPC))
                    amazonNPC.Release(npc);

                orig(npc, position, style, extraInfo);
            };

            // Отправляем информацию подключившемуся игроку обо всех прикреплённых врагах
            ModEvents.OnPlayerConnect += player =>
            {
                if (Main.netMode != NetmodeID.Server)
                    return;

                foreach (var npc in Main.ActiveNPCs)
                {
                    if (!npc.TryGetGlobalNPC(out AmazonGlobalNPC amazonNPC) || amazonNPC.Data is null)
                        continue;

                    NetHandler.Send<AmazonAnchorPacket>(player.whoAmI, null, (byte)npc.whoAmI, (ushort)npc.type, amazonNPC.Data);
                }
            };
        }

        public override void OnKill(NPC npc)
        {
            Release(npc);
        }

        public override bool PreAI(NPC npc)
        {
            if (Data is null)
                return true;

            Data.TimeLeft--;

            if (Main.netMode != NetmodeID.MultiplayerClient)
                CheckAnchor(npc);

            if (Data is not null && Data.TimeLeft <= 0)
                SetAnchorData(npc, null);

            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Data is null)
                return;

            DrawDebug(spriteBatch, Data.WorldPosition - screenPos);
            DrawDebug(spriteBatch, npc.Center + npc.gfxOffY * Vector2.UnitY - screenPos);
        }

        public void TryAnchorFromHit(NPC npc, int damageDone)
        {
            if (!Main.rand.NextBool(AmazonProjectile.AttachChanceDenominator) || !CanBeAnchored(npc))
                return;

            var remainingLife = npc.IsChild(out var parent) ? parent.life : npc.life;

            if (remainingLife <= damageDone * AmazonProjectile.AttachMinRemainingHits)
                return;

            if (AnyAnchoredNear(npc))
                return;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetHandler.Send<AmazonAnchorPacket>(null, null, (byte)npc.whoAmI, (ushort)npc.type);
                return;
            }

            TryAnchor(npc);
        }

        private void TryAnchor(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !CanBeAnchored(npc))
                return;

            if (Data is not null && IsValidAnchorTile(Data.Tile))
            {
                SetAnchorData(npc, new AnchorData(Data.Tile, Data.Length, Math.Max(Data.TimeLeft, AmazonProjectile.AttachDuration)));
                return;
            }

            if (!TileUtils.TryFindClosestTile(npc.Center.ToTileCoordinates(), AmazonProjectile.AttachSearchRadius / TileUtils.TileSizeInPixels, IsValidAnchorTile, out var tile))
                return;

            var tilePos = tile.ToWorldCoordinates();
            var dist = Vector2.Distance(tilePos, npc.Center);

            if (dist >= AmazonProjectile.AttachBreakDistance)
                return;

            var length = MathF.Min(dist + AmazonProjectile.AttachExtraLength, AmazonProjectile.AttachSearchRadius);

            SetAnchorData(npc, new AnchorData(tile, length, AmazonProjectile.AttachDuration));
        }

        private void Release(NPC npc)
        {
            if (Data is null)
                return;

            SetAnchorData(npc, null);
        }

        private void CheckAnchor(NPC npc)
        {
            if (Data is null)
                return;

            if (!IsValidAnchorTile(Data.Tile))
            {
                SetAnchorData(npc, null);
                return;
            }

            // Разрушаем цепь, если НПС слишком далеко от тайла (обычно, это будет происходить при телепортации)
            if (Vector2.Distance(Data.WorldPosition, npc.Center) >= AmazonProjectile.AttachBreakDistance)
            {
                SetAnchorData(npc, null);
                return;
            }

            // Разрушаем цепь для Goblin Sorcerer, Tim, Dark Caster и других схожих врагов перед их телепортацией
            if (npc.aiStyle == NPCAIStyleID.Caster && npc.ai[2] != 0f && npc.ai[3] != 0f)
                SetAnchorData(npc, null);
        }

        private void SetAnchorData(NPC npc, AnchorData data)
        {
            Data = data;

            if (Main.netMode == NetmodeID.Server)
                NetHandler.Send<AmazonAnchorPacket>(null, null, (byte)npc.whoAmI, (ushort)npc.type, Data);
        }

        private void Constrain(NPC npc)
        {
            if (Data is null)
                return;

            var tilePos = Data.WorldPosition;
            var nextPos = npc.Center + npc.velocity;
            var offset = nextPos - tilePos;
            var dist = offset.Length();

            if (dist <= Data.Length)
                return;

            var newPos = tilePos + offset * (Data.Length / dist);
            npc.velocity += newPos - nextPos;
        }

        private static bool CanBeAnchored(NPC npc)
            => npc.CanBeChasedBy() && !npc.IsBossOrRelated() &&
               // Площадь хитбокса не должна быть слишком большой
               (npc.width * npc.height) <= MathF.Pow(TileUtils.TileSizeInPixels * 6f, 2f) &&
               // При этом очень высокие и очень широкие враги тоже в пролете
               npc.width <= TileUtils.TileSizeInPixels * 9f && npc.height <= TileUtils.TileSizeInPixels * 9f;

        private static bool AnyAnchoredNear(NPC target)
        {
            foreach (var npc in Main.ActiveNPCs)
            {
                if (npc.whoAmI == target.whoAmI)
                    continue;

                if (!npc.TryGetGlobalNPC(out AmazonGlobalNPC amazonNPC) || amazonNPC.Data is null)
                    continue;

                if (Vector2.DistanceSquared(npc.Center, target.Center) <= MathF.Pow(AmazonProjectile.AttachChanceReductionDistance, 2))
                    return true;
            }

            return false;
        }

        private static bool IsValidAnchorTile(Point coord)
        {
            if (!WorldGen.InWorld(coord.X, coord.Y))
                return false;

            var tile = Main.tile[coord.X, coord.Y];

            if (!tile.HasUnactuatedTile)
                return false;

            return WorldGen.SolidOrSlopedTile(coord.X, coord.Y) || tile.IsHalfBlock || TileID.Sets.Platforms[tile.TileType];
        }

        private static void DrawDebug(SpriteBatch spriteBatch, Vector2 position)
        {
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, position, new Rectangle(0, 0, 1, 1), Color.Lime, 0f, new Vector2(0.5f, 0.5f), 12f, SpriteEffects.None, 0f);
        }
    }
}