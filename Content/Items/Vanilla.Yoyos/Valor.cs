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

        void ILoadable.Unload() { }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class ValorItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Valor;
    }

    public sealed class ValorProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Valor;

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Ясное дело, требуется добавить шанс нанесения на врага этого баффа
            // + для баланса/красоты (цепей оч много, месиво какое-то), понижать вероятность нанесения баффа если рядом уже есть враги с этим же баффом...
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
            public Point TileCoord { get; init; }
            public float MaxLength { get; init; }
            public PhysicalChain Physics { get; init; }

            public ChainData(Point tileCoord, Vector2 npcPos, float maxLength)
            {
                TileCoord = tileCoord;
                MaxLength = maxLength;

                var nodes = new List<PhysicalChain.Node>();
                var tilePos = tileCoord.ToWorldCoordinates();
                var dirToNPC = Vector2.Normalize(npcPos - tilePos);
                var nodeCount = Math.Max(maxLength / 10f, 2);

                for (int i = 0; i < nodeCount; i++)
                {
                    nodes.Add(new PhysicalChain.Node(tilePos + dirToNPC * i * 9f, false));
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
        public static readonly float ChainAddLength = TileUtils.TileSizeInPixels * 2.5f;
        public static readonly float ChainLengthToBreak = TileUtils.TileSizeInPixels * 12f;
        public static readonly float KnockbackPower = 4f;

        private bool _oldMustBeChained;
        private int _timeSinceLastTileCheck;
        private ChainData _chainData;

        public override bool InstancePerEntity { get => true; }
        public bool IsChained { get => _chainData is not null; }
        public bool MustBeChained { get; private set; }
        public ChainData Data { get => _chainData; private set => _chainData = value; }

        public override void Load()
        {
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
                    if (!npc.TryGetGlobalNPC(out ValorGlobalNPC valorNPC) || !valorNPC.IsChained)
                        return;

                    valorNPC.UpdateCollision(npc);
                });
            };

            // При любой телепортации НПС разрушаем ёё
            On_NPC.Teleport += (orig, npc, position, style, extraInfo) =>
            {
                if (npc.TryGetGlobalNPC(out ValorGlobalNPC valorNPC))
                    valorNPC.BreakChain(npc);

                orig(npc, position, style, extraInfo);
            };
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (!CanBeChained(npc))
            {
                npc.buffImmune[ModContent.BuffType<ValorBuff>()] = true;
            }
        }

        public override void OnKill(NPC npc)
        {
            BreakChain(npc);
        }

        public override bool PreAI(NPC npc)
        {
            // Обновляем информацию о баффе
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                var hasBuff = npc.HasBuff<ValorBuff>();

                if (MustBeChained != hasBuff)
                {
                    MustBeChained = hasBuff;
                    npc.netUpdate = true;
                }
            }

            // Меняем состояние эффекта от баффа
            if (_oldMustBeChained != MustBeChained)
            {
                if (MustBeChained)
                {
                    ValorNPCVisualEffectHandler.AddNPC(npc);
                    ChainToTile(npc);
                }
                else
                {
                    ValorNPCVisualEffectHandler.RemoveNPC(npc);
                    BreakChain(npc);
                }

                _oldMustBeChained = MustBeChained;
            }

            // Если эффект отсутствует, прекращаем функцию
            if (!MustBeChained)
                return true;
            // Иначе, если эффект есть ...

            // ... но НПС не зацеплен, то:
            if (!IsChained)
            {
                // Периодически пытаемся приципить врага к плитке, если он еще не закреплен
                if (_timeSinceLastTileCheck++ < TileCheckFrequency)
                {
                    _timeSinceLastTileCheck = 0;

                    ChainToTile(npc);
                }

                return true;
            }

            // ... и НПС зацеплен, то ...

            // ... обновляем физику цепи, ...
            if ((npc.whoAmI + Main.GameUpdateCount) % 2 == 0)
                Data.Physics.Simulate(Data.TileCoord.ToWorldCoordinates(), npc.Center, 10);

            // ... разрушаем цепь, если НПС слишком далеко от тайла
            if (Vector2.Distance(Data.TileCoord.ToWorldCoordinates(), npc.Center) >= ChainLengthToBreak)
            {
                BreakChain(npc);
                return true;
            }

            // ... разрушаем цепь для Goblin Sorcerer, Tim, Dark Caster и других перед телепортацией
            if (npc.aiStyle == NPCAIStyleID.Caster && npc.ai[2] != 0f && npc.ai[3] != 0f)
            {
                BreakChain(npc);
                return true;
            }

            return true;
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(MustBeChained);

            if (!MustBeChained)
                return;

            bitWriter.WriteBit(IsChained);

            if (!IsChained)
            {
                binaryWriter.Write((short)_timeSinceLastTileCheck);
                return;
            }

            binaryWriter.Write((short)Data.TileCoord.X);
            binaryWriter.Write((short)Data.TileCoord.Y);
            binaryWriter.Write(Data.MaxLength);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            MustBeChained = bitReader.ReadBit();

            if (!MustBeChained)
                return;

            var isChained = bitReader.ReadBit();

            if (!isChained)
            {
                _timeSinceLastTileCheck = binaryReader.ReadInt16();
                return;
            }

            var tileCoord = new Point(binaryReader.ReadInt16(), binaryReader.ReadInt16());
            var maxLength = binaryReader.ReadSingle();

            if (Data is not null && Data.TileCoord == tileCoord && Data.MaxLength == maxLength)
                return;

            Data = new ChainData(tileCoord, npc.Center, maxLength);
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Data is null)
                return;

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, Data.TileCoord.ToWorldCoordinates(0, 0) - Main.screenPosition, new Rectangle(0, 0, 16, 16), Color.Blue);
        }

        private bool ChainToTile(NPC npc)
        {
            if (IsChained)
                return false;

            if (!TryFindSuitableTile(npc, out var tileCoord))
                return false;

            var tilePos = tileCoord.ToWorldCoordinates();
            var distFromNPCToTile = Vector2.Distance(tilePos, npc.Center);

            if (distFromNPCToTile >= ChainLengthToBreak)
                return false;

            SoundEngine.PlaySound(SoundID.Unlock, npc.Center);

            npc.velocity += Vector2.Normalize(npc.Center - tilePos) * KnockbackPower;

            Data = new ChainData(tileCoord, npc.Center, MathF.Min(distFromNPCToTile + ChainAddLength, TileCheckRadius * TileUtils.TileSizeInPixels));
            npc.netUpdate = true;

            return true;
        }

        private bool BreakChain(NPC npc)
        {
            if (!IsChained)
                return false;

            SoundEngine.PlaySound(SoundID.Unlock, npc.Center);

            Data = null;
            npc.netUpdate = true;

            return true;
        }

        private void UpdateCollision(NPC npc)
        {
            var chainPosition = Data.TileCoord.ToWorldCoordinates();

            var nextPosition = npc.Center + npc.velocity;
            var vectorFromChainToNPC = nextPosition - chainPosition;
            var vectorFromChainToNPCLength = vectorFromChainToNPC.Length();

            if (vectorFromChainToNPCLength <= Data.MaxLength)
                return;

            var normalizedVectorFromChainToNPC = Vector2.Normalize(vectorFromChainToNPC);
            var newPosition = chainPosition + normalizedVectorFromChainToNPC * Data.MaxLength;
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
        private readonly NPCObserver _npcObserver = new(n => !n.TryGetGlobalNPC(out ValorGlobalNPC valorNPC) || !valorNPC.MustBeChained);

        private bool _targetWasPrepared = false;

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
            ModEvents.OnPostUpdateCameraPosition -= DrawNPCsToTarget;
            ModEvents.OnPostUpdateEverything -= _npcObserver.Update;
        }

        public static void AddNPC(NPC npc)
            => ModContent.GetInstance<ValorNPCVisualEffectHandler>()?._npcObserver.Add(npc);

        public static void RemoveNPC(NPC npc)
            => ModContent.GetInstance<ValorNPCVisualEffectHandler>()?._npcObserver.Remove(npc);

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
                var texture = TextureAssets.Chain22;

                foreach (var npc in _npcObserver.GetEntityInstances())
                {
                    var endPosition = (npc.Center + npc.gfxOffY * Vector2.UnitY - Main.screenPosition);
                    var chainData = npc.GetGlobalNPC<ValorGlobalNPC>().Data;

                    if (chainData is null)
                        continue;

                    // При телепортации тех же шаманов гоблинов вылазит null ошибка, так что над продумать это
                    var startPosition = chainData.TileCoord.ToWorldCoordinates() - Main.screenPosition;
                    var vectorFromChainToNPC = endPosition - startPosition;
                    var vectorFromChainToNPCLength = (int)vectorFromChainToNPC.Length();

                    var segmentRotation = vectorFromChainToNPC.ToRotation() + MathHelper.PiOver2;
                    var segmentOrigin = texture.Size() * 0.5f;
                    var segmentCount = (int)Math.Ceiling((float)vectorFromChainToNPCLength / texture.Width());
                    var segmentVector = Vector2.Normalize(vectorFromChainToNPC) * texture.Width();

                    /*for (var i = 0; i < segmentCount; i++)
                    {
                        var position = startPosition + segmentVector * i;
                        var color = Lighting.GetColor((position + Main.screenPosition).ToTileCoordinates());
                        Main.spriteBatch.Draw(texture.Value, position, null, color, segmentRotation, segmentOrigin, 1f, SpriteEffects.None, 0);
                    }*/

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