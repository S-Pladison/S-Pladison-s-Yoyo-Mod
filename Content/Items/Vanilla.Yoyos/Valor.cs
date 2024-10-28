using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using SPYoyoMod.Common.Graphics.RenderTargets;
using SPYoyoMod.Utils;
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

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class ValorAssets : ILoadable
    {
        // [ Текстуры ]
        public const string InvisiblePath = $"{_assetPath}Invisible";
        public const string BuffPath = $"{_valorPath}ValorBuff";

        // [ Эффекты ]
        public static Asset<Effect> NPCOutlineEffect { get; private set; } = ModContent.Request<Effect>($"{_valorPath}ValorNPCOutline");

        // [ Общее ]
        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _valorPath = $"{_assetPath}Items/Vanilla.Yoyos/Valor/";

        void ILoadable.Unload()
        {
            NPCOutlineEffect = null;
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class ValorItem : VanillaYoyoBaseItem
    {
        public static readonly int DebuffApplyChanceDenominator = 9;
        public static readonly float DebuffChanceReductionDistance = MathF.Pow(TileUtils.TileSizeInPixels * 12f, 2f); //< Возводим в степень из-за использования DistanceSquared

        public override int ItemType => ItemID.Valor;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Math.Ceiling(1.0f / DebuffApplyChanceDenominator * 100.0f));

        public override void SetDefaults(Item item)
        {
            item.knockBack = 4.5f;
        }
    }

    public sealed class ValorProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Valor;

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.rand.NextBool(ValorItem.DebuffApplyChanceDenominator))
                return;

            foreach (var npc in Main.ActiveNPCs)
            {
                if (!npc.HasBuff<ValorBuff>())
                    continue;

                if (npc.whoAmI == target.whoAmI)
                    continue;

                if (Vector2.DistanceSquared(npc.Center, target.Center) <= ValorItem.DebuffChanceReductionDistance)
                    return;
            }

            target.AddBuff(ModContent.BuffType<ValorBuff>(), ModUtils.SecondsToTicks(7f));
        }
    }

    public sealed class ValorBuff : ModBuff
    {
        public override string Texture => ValorAssets.BuffPath;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
        }
    }

    public sealed class ValorGlobalNPC : GlobalNPC
    {
        public sealed class ChainData
        {
            public Point Tile;
            public float Length;
            public PhysicalChain Physics;
            public ushort LifeTime;

            public ChainData(Point tile, NPC npc, float length, ushort lifeTime = 0)
            {
                Tile = tile;
                Length = length;
                LifeTime = lifeTime;

                var nodes = new List<PhysicalChain.Node>();
                var tilePos = tile.ToWorldCoordinates();
                var dirToNPC = Vector2.Normalize(npc.Center - tilePos);
                var nodeCount = Math.Max(length / 12f, 2);

                for (int i = 0; i < nodeCount; i++)
                {
                    nodes.Add(new PhysicalChain.Node(tilePos + dirToNPC * i * 12f, false));
                }

                Physics = new(nodes)
                {
                    DistanceBetweenNodes = 8f,
                    Gravity = Vector2.UnitY * 3f
                };
            }
        }

        public static readonly int TileCheckFrequency = ModUtils.SecondsToTicks(1f);
        public static readonly int TileCheckRadius = 7;
        public static readonly float ChainAdditionalLength = TileUtils.TileSizeInPixels * 2.5f;
        public static readonly float ChainLengthToBreak = TileUtils.TileSizeInPixels * 12f;

        private bool _prevHasDebuff;
        private int _timeSinceLastTileCheck;

        public override bool InstancePerEntity { get => true; }
        public bool HasDebuff { get; private set; }
        public ChainData Data { get; private set; }

        public override void Load()
        {
            // Коллизия, ограничивающая перемещение НПС в определенном радиусе
            // Из-за мелких артефактов по типу тряски и т.п., решил что лучше решения не будет
            IL_NPC.UpdateNPC_Inner += (il) =>
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
                    ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(ValorGlobalNPC)}..{nameof(IL_NPC.UpdateNPC_Inner)}\" failed...");
                    return;
                }

                cursor.Index--;

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<NPC>>(npc =>
                {
                    if (!npc.TryGetGlobalNPC(out ValorGlobalNPC valorNPC))
                        return;

                    valorNPC.UpdateCollision(npc);
                });
            };

            // При любой телепортации НПС разрушаем цепь
            On_NPC.Teleport += (orig, npc, position, style, extraInfo) =>
            {
                if (npc.TryGetGlobalNPC(out ValorGlobalNPC valorNPC))
                    valorNPC.BreakChain(npc);

                orig(npc, position, style, extraInfo);
            };
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(HasDebuff);

            if (!HasDebuff)
                return;

            bitWriter.WriteBit(Data is null);

            if (Data is null)
                return;

            binaryWriter.Write((short)Data.Tile.X);
            binaryWriter.Write((short)Data.Tile.Y);
            binaryWriter.Write(Data.Length);
            binaryWriter.Write(Data.LifeTime);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            HasDebuff = bitReader.ReadBit();

            if (!HasDebuff)
            {
                // Я бы хотел это убрать, но не могу...
                SetChainData(npc, null);
                return;
            }

            if (bitReader.ReadBit())
            {
                SetChainData(npc, null);
                return;
            }

            var tile = new Point(binaryReader.ReadInt16(), binaryReader.ReadInt16());
            var length = binaryReader.ReadSingle();
            var lifeTime = binaryReader.ReadUInt16();

            if (Data is null)
            {
                SetChainData(npc, new ChainData(tile, npc, length, lifeTime));
                return;
            }

            Data.Tile = tile;
            Data.Length = length;
            Data.LifeTime = lifeTime;
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (HasDebuff)
                drawColor = NPC.buffColor(drawColor, 0.35f, 0.65f, 1.0f, 1.0f);
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (!CanBeChained(npc))
                npc.buffImmune[ModContent.BuffType<ValorBuff>()] = true;
        }

        public override void OnKill(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                SetChainData(npc, null);
                return;
            }

            BreakChain(npc);
        }

        public override bool PreAI(NPC npc)
        {
            // Обновляем физику цепи и счетчик жизни
            if (Data is not null)
            {
                Data.LifeTime++;

                if ((npc.whoAmI + Main.GameUpdateCount) % 2 == 0)
                    Data.Physics.Simulate(Data.Tile.ToWorldCoordinates(), npc.Center, 5);
            }

            HandleChain(npc);

            // Ведем учет нпс с дебаффом для дальнейшей отрисовки
            if (_prevHasDebuff != HasDebuff)
            {
                if (HasDebuff)
                    ValorNPCVisualEffectHandler.AddNPC(npc);
                else
                    ValorNPCVisualEffectHandler.RemoveNPC(npc);

                _prevHasDebuff = HasDebuff;
            }

            return true;
        }

        private void HandleChain(NPC npc)
        {
            // Вся основная логика должна происходить только в сингле на клиенте (что логично) и на сервере в мультиплеере
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            var hasBuff = npc.HasBuff<ValorBuff>();

            if (HasDebuff != hasBuff)
            {
                if (hasBuff)
                    ChainToTile(npc);
                else
                    BreakChain(npc);

                HasDebuff = hasBuff;
                npc.netUpdate = true;
            }

            if (!HasDebuff)
                return;

            // Если НПС все еще не прикреплен к плитке (в случаех, если он слишком далеко или телепортировался)
            if (Data is null && _timeSinceLastTileCheck++ >= TileCheckFrequency)
            {
                ChainToTile(npc);
                _timeSinceLastTileCheck = 0;
                return;
            }

            // Разрушаем цепь, если НПС слишком далеко от тайла (обычно, это будет происходить при телепортации)
            if (Vector2.Distance(Data.Tile.ToWorldCoordinates(), npc.Center) >= ChainLengthToBreak)
            {
                BreakChain(npc);
                return;
            }

            // Разрушаем цепь для Goblin Sorcerer, Tim, Dark Caster и других схожих врагов перед их телепортацией
            if (npc.aiStyle == NPCAIStyleID.Caster && npc.ai[2] != 0f && npc.ai[3] != 0f)
            {
                BreakChain(npc);
                return;
            }
        }

        private void SetChainData(NPC npc, ChainData data)
        {
            if (data is not null)
            {
                if (Data is null)
                    SoundEngine.PlaySound(SoundID.Unlock, npc.Center);

                Data = data;
                return;
            }

            if (Data is not null)
                SoundEngine.PlaySound(SoundID.Unlock, npc.Center);

            Data = null;
        }

        private bool ChainToTile(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return false;

            if (Data is not null)
                return false;

            if (!TryFindSuitableTile(npc, out var tileCoord))
                return false;

            var tilePos = tileCoord.ToWorldCoordinates();
            var distFromNPCToTile = Vector2.Distance(tilePos, npc.Center);

            if (distFromNPCToTile >= ChainLengthToBreak)
                return false;

            SetChainData(npc, new ChainData(tileCoord, npc, MathF.Min(distFromNPCToTile + ChainAdditionalLength, TileCheckRadius * TileUtils.TileSizeInPixels)));

            npc.netUpdate = true;
            return true;
        }

        private bool BreakChain(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return false;

            if (Data is null)
                return false;

            SetChainData(npc, null);

            npc.netUpdate = true;
            return true;
        }

        private void UpdateCollision(NPC npc)
        {
            if (Data is null)
                return;

            var chainPosition = Data.Tile.ToWorldCoordinates();

            var nextPosition = npc.Center + npc.velocity;
            var vectorFromChainToNPC = nextPosition - chainPosition;
            var vectorFromChainToNPCLength = vectorFromChainToNPC.Length();

            if (vectorFromChainToNPCLength <= Data.Length)
                return;

            var normalizedVectorFromChainToNPC = Vector2.Normalize(vectorFromChainToNPC);
            var newPosition = chainPosition + normalizedVectorFromChainToNPC * Data.Length;
            var velocityCorrection = newPosition - nextPosition;

            npc.velocity += velocityCorrection;
        }

        private static bool CanBeChained(NPC npc)
            => npc.CanBeChasedBy() &&
                !npc.IsBossOrRelated() &&
                // Площадь хитбокса не должна быть слишком большой
                (npc.width * npc.height) <= MathF.Pow(TileUtils.TileSizeInPixels * 6f, 2f) &&
                // При этом очень высокие и очень широкие враги тоже в пролете
                npc.width <= TileUtils.TileSizeInPixels * 9f &&
                npc.height <= TileUtils.TileSizeInPixels * 9f;

        private static bool TryFindSuitableTile(NPC npc, out Point tileCoord)
            => TileUtils.TryFindClosestTile(
                centerCoord: npc.Center.ToTileCoordinates(),
                tilesFromCenter: TileCheckRadius,
                predicate: t => WorldGen.SolidOrSlopedTile(t.X, t.Y) || Main.tile[t.X, t.Y].IsHalfBlock || TileID.Sets.Platforms[Main.tile[t.X, t.Y].TileType],
                tileCoord: out tileCoord);
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class ValorNPCVisualEffectHandler : ILoadable
    {
        private readonly ScreenRenderTarget _renderTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.Default);
        private readonly NPCObserver _npcObserver = new(n => !n.TryGetGlobalNPC(out ValorGlobalNPC valorNPC) || !valorNPC.HasDebuff);

        private bool _targetWasPrepared = false;

        public static void AddNPC(NPC npc)
            => ModContent.GetInstance<ValorNPCVisualEffectHandler>()?._npcObserver.Add(npc);

        public static void RemoveNPC(NPC npc)
            => ModContent.GetInstance<ValorNPCVisualEffectHandler>()?._npcObserver.Remove(npc);

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            ModEvents.OnPostUpdateEverything += _npcObserver.Update;
            ModEvents.OnPostUpdateCameraPosition += DrawNPCsToTarget;
            ModEvents.OnPreDraw += EmitLight;

            On_Main.DoDraw_Tiles_NonSolid += (orig, main) =>
            {
                orig(main);
                DrawOutlineToScreen();
            };

            On_Main.DrawNPCs += (orig, main, behindTiles) =>
            {
                orig(main, behindTiles);

                if (behindTiles)
                    return;

                DrawChains();
            };
        }

        void ILoadable.Unload()
        {
            ModEvents.OnPreDraw -= EmitLight;
            ModEvents.OnPostUpdateCameraPosition -= DrawNPCsToTarget;
            ModEvents.OnPostUpdateEverything -= _npcObserver.Update;
        }

        private void DrawNPCsToTarget()
        {
            if (!_npcObserver.AnyEntity)
                return;

            _targetWasPrepared = false;

            var device = Main.graphics.GraphicsDevice;
            device.SetRenderTarget(_renderTarget);
            device.Clear(Color.Transparent);
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                foreach (var npc in _npcObserver.GetEntityInstances())
                    NPCUtils.DrawNPC(npc);

                Main.spriteBatch.End();
            }
            device.SetRenderTarget(null);

            _targetWasPrepared = true;
        }

        private void EmitLight()
        {
            // - Почему в PreDraw, а не в Update или гдет еще?
            // В паузе источники освещения из Update не появляются...
            // Да, костыль, но надеюсь он ни на что не повлияет

            if (!_npcObserver.AnyEntity)
                return;

            // Lighting.AddLight(...)
            // {
            //     if (!Main.gamePaused && Main.netMode != 2)
            //     {
            //         _activeEngine.AddLight(...);
            //     }
            // }

            var origGamePaused = Main.gamePaused;
            Main.gamePaused = false;

            foreach (var npc in _npcObserver.GetEntityInstances())
                Lighting.AddLight(npc.Center, new Color(35, 90, 255).ToVector3() * 0.3f);

            Main.gamePaused = origGamePaused;
        }

        private void DrawOutlineToScreen()
        {
            if (!_targetWasPrepared)
                return;

            var effect = ValorAssets.NPCOutlineEffect.Prepare(parameters =>
            {
                parameters["ScreenSize"].SetValue(_renderTarget.Size);
                parameters["OutlineColor"].SetValue(new Color(18, 75, 210).ToVector4());
                parameters["Zoom"].SetValue(new Vector2(Main.GameZoomTarget));
            });

            Main.spriteBatch.End(out var spriteBatchSnapshot);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect.Value, Matrix.Identity);
            Main.spriteBatch.Draw(_renderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(spriteBatchSnapshot);

            _targetWasPrepared = false;
        }

        private void DrawChains()
        {
            if (!_npcObserver.AnyEntity)
                return;

            Main.spriteBatch.End(out var spriteBatchSnapshot);
            Main.spriteBatch.Begin(spriteBatchSnapshot with { Effect = null });
            {
                foreach (var npc in _npcObserver.GetEntityInstances())
                {
                    if (!npc.TryGetGlobalNPC<ValorGlobalNPC>(out var valorNPC) || valorNPC.Data is null)
                        continue;

                    var chainData = valorNPC.Data;

                    var startPosition = chainData.Tile.ToWorldCoordinates() - Main.screenPosition;
                    var endPosition = (npc.Center + npc.gfxOffY * Vector2.UnitY - Main.screenPosition);
                    var vectorFromChainToNPC = endPosition - startPosition;
                    var vectorFromChainToNPCLength = (int)vectorFromChainToNPC.Length();

                    /*var segmentRotation = vectorFromChainToNPC.ToRotation() + MathHelper.PiOver2;
                    var segmentOrigin = texture.Size() * 0.5f;
                    var segmentCount = (int)Math.Ceiling((float)vectorFromChainToNPCLength / texture.Width());
                    var segmentVector = Vector2.Normalize(vectorFromChainToNPC) * texture.Width();*/

                    foreach (var nodePosition in chainData.Physics.GetPositions())
                    {
                        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, nodePosition - Main.screenPosition, new Rectangle(-1, -1, 1, 1), Color.Lime);
                    }
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(spriteBatchSnapshot);
        }
    }
}