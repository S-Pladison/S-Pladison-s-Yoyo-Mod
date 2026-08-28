using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using SPYoyoMod.Common.Yoyos;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Graphics.RenderTargets;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Core.Netcode;
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

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class ValorAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Valor/Valor";

        public const string InvisiblePath = $"{AssetPath}/Invisible";
        public const string BuffPath = $"{YoyoPath}Buff";
        public const string StringPath = $"{AssetPath}/FishingLine_WithShadow";

        public static readonly LazyAsset<Texture2D> GlowTexture = LazyAsset<Texture2D>.From($"{AssetPath}/YoyoGlow_WithShadow");
        public static readonly LazyAsset<Texture2D> NoiseTexture = LazyAsset<Texture2D>.From($"{AssetPath}/CloudNoise");
        public static readonly LazyAsset<Effect> TrailEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Trail");
        public static readonly LazyAsset<Effect> OutlineEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Outline");
        public static readonly SoundStyle ChainSound = SoundID.Unlock;
    }

    public sealed class ValorItem : YoyoItem<ValorProjectile>
    {
        public override int OverrideType => ItemID.Valor;

        //=/-

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Math.Ceiling(1.0f / ValorProjectile.DebuffApplyChanceDenominator * 100.0f));

        public override void SetDefaults(Item item)
        {
            item.knockBack = 4.5f;
        }
    }

    public sealed class ValorProjectile : YoyoProjectile<ValorItem>, IInitializableProjectile, IEmitLightEntity, IPreDrawPixelatedProjectile, IHaveHitEffectProjectile
    {
        public override int OverrideType => ProjectileID.Valor;

        //=/-

        public static readonly int DebuffApplyChanceDenominator = 9;
        public static readonly float DebuffChanceReductionDistance = MathF.Pow(TileUtils.TileSizeInPixels * 12f, 2f); //< Возводим в степень из-за использования DistanceSquared
        public static readonly Color GlowColor = new(35, 90, 255);
        public static readonly int TrailPointCount = 7;

        private YoyoStringRenderer _stringRenderer;
        private StripRenderer _trailRenderer;
        private LinkedList<Vector2> _oldPositions;

        void IInitializableProjectile.Initialize(Projectile proj)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            _stringRenderer = new YoyoStringRenderer(new IDrawYoyoStringSegments.Gradient(
               ModContent.Request<Texture2D>(ValorAssets.StringPath, AssetRequestMode.ImmediateLoad).Value,
               (Color.Transparent, true), (GlowColor, true)
            ));

            _trailRenderer = new StripRenderer(Main.graphics.GraphicsDevice, capacity: TrailPointCount)
            {
                StartWidth = 16,
                EndWidth = 8,
                StartColor = GlowColor,
                EndColor = Color.Transparent
            };

            _oldPositions = [];
        }

        public override void OnKill(Projectile proj, int timeLeft)
        {
            _trailRenderer?.Dispose();
        }

        public override void AI(Projectile proj)
        {
            if (_trailRenderer is not null)
            {
                _oldPositions.AddFirst(proj.Center + proj.velocity);

                while (_oldPositions.Count > TrailPointCount)
                    _oldPositions.RemoveLast();

                _trailRenderer.SetPoints(_oldPositions);
            }

            if (Main.rand.NextBool(3))
            {
                var dust = Main.dust[Dust.NewDust(proj.position, proj.width, proj.height, Main.rand.NextBool() ? DustID.DungeonWater : DustID.WaterCandle)];
                dust.noGravity = true;
                dust.noLightEmittence = true;
            }
        }

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.rand.NextBool(DebuffApplyChanceDenominator))
                return;

            foreach (var npc in Main.ActiveNPCs)
            {
                if (!npc.HasBuff<ValorBuff>())
                    continue;

                if (npc.whoAmI == target.whoAmI)
                    continue;

                if (Vector2.DistanceSquared(npc.Center, target.Center) <= DebuffChanceReductionDistance)
                    return;
            }

            target.AddBuff(ModContent.BuffType<ValorBuff>(), GeneralUtils.SecondsToTicks(7f));
        }

        void IHaveHitEffectProjectile.HitEffect(Projectile proj, NPC target, NPC.HitInfo hit)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 5; i++)
            {
                var dust = Main.dust[Dust.NewDust(proj.position, proj.width, proj.height, Main.rand.NextBool() ? DustID.DungeonWater : DustID.WaterCandle)];
                dust.noGravity = true;
                dust.noLightEmittence = true;
                dust.velocity = Vector2.Normalize(proj.Center - target.Center).RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * Main.rand.NextFloat(1.5f, 4.0f);
            }
        }

        void IEmitLightEntity.EmitLight(Entity entity)
        {
            Lighting.AddLight(entity.Center, GlowColor.ToVector3() * 0.2f);
        }

        void IPreDrawPixelatedProjectile.PreDrawPixelated(Projectile proj)
        {
            if (_trailRenderer is null)
                return;

            ValorAssets.TrailEffect
                .Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(TextureAssets.MagicPixel.Value);
                    parameters["TransformMatrix"].SetValue(GameMatrices.World * GameMatrices.Effect * GameMatrices.Projection);
                })
                .Apply();

            _trailRenderer.Render();
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            var glowPosition = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var glowTexture = ValorAssets.GlowTexture.Value;
            var glowOrigin = glowTexture.Size() * 0.5f;
            var glowScale = proj.scale * 1.2f;

            Main.spriteBatch.Draw(glowTexture, glowPosition, null, GlowColor, proj.rotation, glowOrigin, glowScale, SpriteEffects.None, 0f);

            return true;
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            if (_stringRenderer is null)
                return;

            _stringRenderer.Render(Main.spriteBatch, YoyoStringRendererContext.FromProjectile(proj, mountedCenter));
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

    public sealed class ValorGlobalNPC : GlobalNPC, IEmitLightEntity
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
                var nodeCount = Math.Max(length / 10f, 2);

                for (int i = 0; i < nodeCount; i++)
                {
                    nodes.Add(new PhysicalChain.Node(tilePos + dirToNPC * i * 10f));
                }

                Physics = new(nodes)
                {
                    DistanceBetweenNodes = 7f,
                    Gravity = Vector2.UnitY * 3f
                };
            }
        }

        // Отравляем с сервера клиентам в случаях, когда зацепили/отцепили NPC
        private sealed class NPCValorSyncPacket : NetPacket
        {
            public override void Send(BinaryWriter writer, params object[] context)
            {
                // Данные об NPC
                writer.Write((byte)context[0]); //< npcWhoAmI
                writer.Write((ushort)context[1]); //< npcType

                var chainData = context[2] as ChainData;

                // Данные о цепи
                writer.Write(chainData is not null);
                if (chainData is not null)
                {
                    writer.Write((ushort)chainData.Tile.X);
                    writer.Write((ushort)chainData.Tile.Y);
                    writer.Write(chainData.Length);
                    writer.Write(chainData.LifeTime);
                }
            }

            public override void Receive(BinaryReader reader, int sender)
            {
                var npcWhoImA = reader.ReadByte();
                var npcType = reader.ReadUInt16();
                var hasChainData = reader.ReadBoolean();
                var npc = Main.npc[npcWhoImA];

                if (npc is null || npc.type != npcType || !npc.TryGetGlobalNPC<ValorGlobalNPC>(out var globalNPC))
                    return;

                if (!hasChainData)
                {
                    globalNPC.SetChainData(npc, null);
                    return;
                }

                var chainTile = new Point(reader.ReadUInt16(), reader.ReadUInt16());
                var chainLength = reader.ReadSingle();
                var chainLifeTime = reader.ReadUInt16();

                globalNPC.SetChainData(npc, new ChainData(chainTile, npc, chainLength, chainLifeTime));
            }
        }

        // Отправляем с сервера только что подключившемуся клиенту для синхронизации данных обо всех подцепленных NPC
        private sealed class NPCValorSyncAllPacket : NetPacket
        {
            public override void Send(BinaryWriter writer, params object[] context)
            {
                var valorNPCs = new List<(NPC npc, ChainData data)>();

                foreach (var npc in Main.ActiveNPCs)
                {
                    if (!npc.TryGetGlobalNPC<ValorGlobalNPC>(out var globalNPC) || !npc.HasBuff<ValorBuff>())
                        continue;

                    valorNPCs.Add((npc, globalNPC.Data));
                }

                writer.Write((byte)valorNPCs.Count);

                // Чаще всего таких NPC будет 0... Ну, к максимум 1-2...
                foreach (var (npc, chainData) in valorNPCs)
                {
                    // Данные об NPC
                    writer.Write((byte)npc.whoAmI);
                    writer.Write((ushort)npc.type);
                    writer.Write((ushort)npc.buffTime[npc.FindBuffIndex<ValorBuff>()]);

                    // Данные о цепи
                    writer.Write(chainData is not null);
                    if (chainData is not null)
                    {
                        writer.Write((ushort)chainData.Tile.X);
                        writer.Write((ushort)chainData.Tile.Y);
                        writer.Write(chainData.Length);
                        writer.Write(chainData.LifeTime);
                    }
                }
            }

            public override void Receive(BinaryReader reader, int sender)
            {
                var valorNPCCount = reader.ReadByte();

                // Чаще всего таких NPC будет 0... Ну, к максимум 1-2...
                for (var index = 0; index < valorNPCCount; index++)
                {
                    var npcWhoAmI = reader.ReadByte();
                    var npcType = reader.ReadUInt16();
                    var npcDebuffTime = reader.ReadUInt16();
                    var hasChainData = reader.ReadBoolean();
                    var npc = Main.npc[npcWhoAmI];

                    if (npc is null || npc.type != npcType || !npc.TryGetGlobalNPC<ValorGlobalNPC>(out var globalNPC))
                        continue;

                    // При подключении игрок не знает о дебаффах врагов
                    npc.AddBuff<ValorBuff>(npcDebuffTime);
                    globalNPC.HasDebuff = true;

                    if (!hasChainData)
                        continue;

                    var chainTile = new Point(reader.ReadUInt16(), reader.ReadUInt16());
                    var chainLength = reader.ReadSingle();
                    var chainLifeTime = reader.ReadUInt16();

                    globalNPC.SetChainData(npc, new ChainData(chainTile, npc, chainLength, chainLifeTime));
                }
            }
        }

        public static readonly int TileCheckFrequency = GeneralUtils.SecondsToTicks(1f);
        public static readonly int TileCheckRadius = 7;
        public static readonly float ChainAdditionalLength = TileUtils.TileSizeInPixels * 2.5f;
        public static readonly float ChainLengthToBreak = TileUtils.TileSizeInPixels * 12f;

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
                    ModLogger.Warn($"IL edit \"{nameof(ValorGlobalNPC)}..{nameof(IL_NPC.UpdateNPC_Inner)}\" failed...");
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

            // Отправляем информацию подключившемуся игроку обо всех прицепленных врагах
            ModEvents.OnPlayerConnect += (Player player) =>
            {
                if (Main.netMode == NetmodeID.Server)
                    NetHandler.Send<NPCValorSyncAllPacket>(player.whoAmI, null);
            };
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
            UpdateDebuffState(npc, npc.HasBuff<ValorBuff>());

            if (!HasDebuff)
                return true;

            if (Main.rand.NextBool(4))
            {
                var dust = Main.dust[Dust.NewDust(npc.position, npc.width, npc.height, Main.rand.NextBool() ? DustID.DungeonWater : DustID.WaterCandle)];
                dust.noGravity = true;
                dust.noLightEmittence = true;
            }

            // Обновляем физику цепи и счетчик жизни
            if (Data is not null)
            {
                Data.LifeTime++;

                if ((npc.whoAmI + Main.GameUpdateCount) % 2 == 0)
                    Data.Physics.Simulate(Data.Tile.ToWorldCoordinates(), npc.Center, 5);
            }

            HandleChain(npc);

            return true;
        }

        private void UpdateDebuffState(NPC npc, bool state)
        {
            if (HasDebuff == state)
                return;

            HasDebuff = state;

            if (state)
            {
                ChainToTile(npc);
                ModContent.GetInstance<ValorNPCOutlineEffectHandler>()?.Add(npc);
            }
            else
            {
                BreakChain(npc);
                ModContent.GetInstance<ValorNPCOutlineEffectHandler>()?.Remove(npc);
            }
        }

        private void HandleChain(NPC npc)
        {
            // Вся основная логика должна происходить только в сингле на клиенте (что логично) и на сервере в мультиплеере
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Если НПС все еще не прикреплен к плитке (в случаех, если он слишком далеко или телепортировался)
            if (Data is null && _timeSinceLastTileCheck++ >= TileCheckFrequency)
            {
                ChainToTile(npc);
                _timeSinceLastTileCheck = 0;
                return;
            }

            // Если всетки не удалось приципить к плитке...
            if (Data is null)
                return;

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

        private void SetChainData(NPC npc, ChainData updatedData)
        {
            if (updatedData is not null)
            {
                if (Data is null)
                {
                    SoundEngine.PlaySound(ValorAssets.ChainSound, npc.Center);
                    Data = updatedData;
                    return;
                }

                Data.Tile = updatedData.Tile;
                Data.Length = updatedData.Length;
                Data.LifeTime = updatedData.LifeTime;
                return;
            }

            if (Data is not null)
            {
                SoundEngine.PlaySound(ValorAssets.ChainSound, npc.Center);
                _timeSinceLastTileCheck = TileCheckFrequency;
                Data = null;
                return;
            }

            Data = null;
        }

        private bool ChainToTile(NPC npc)
        {
            // Вся основная логика должна происходить только в сингле на клиенте (что логично) и на сервере в мультиплеере
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

            var chainData = new ChainData(tileCoord, npc, MathF.Min(distFromNPCToTile + ChainAdditionalLength, TileCheckRadius * TileUtils.TileSizeInPixels));

            SetChainData(npc, chainData);

            if (Main.netMode == NetmodeID.Server)
                NetHandler.Send<NPCValorSyncPacket>(null, null, (byte)npc.whoAmI, (ushort)npc.type, chainData);

            npc.netUpdate = true;
            return true;
        }

        private bool BreakChain(NPC npc)
        {
            // Вся основная логика должна происходить только в сингле на клиенте (что логично) и на сервере в мультиплеере
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return false;

            if (Data is null)
                return false;

            SetChainData(npc, null);

            if (Main.netMode == NetmodeID.Server)
                NetHandler.Send<NPCValorSyncPacket>(null, null, (byte)npc.whoAmI, (ushort)npc.type, null);

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

        void IEmitLightEntity.EmitLight(Entity npc)
        {
            if (!HasDebuff)
                return;

            Lighting.AddLight(npc.Center, new Color(35, 90, 255).ToVector3() * 0.3f);
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
    public sealed class ValorNPCOutlineEffectHandler : ILoadable
    {
        private readonly ScreenRenderTarget _renderTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.Default);
        private readonly NPCObserver _npcObserver = NPCObserver.Create(n => !n.TryGetGlobalNPC(out ValorGlobalNPC valorNPC) || !valorNPC.HasDebuff);

        private bool _targetWasPrepared = false;

        public void Add(NPC npc)
        {
            _npcObserver.Add(npc);
        }

        public void Remove(NPC npc)
        {
            _npcObserver.Remove(npc);
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            ModEvents.OnPostUpdateCameraPosition += DrawNPCsToTarget;

            On_Main.DoDraw_DrawNPCsOverTiles += (orig, main) =>
            {
                DrawOutlineToScreen();
                orig(main);
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
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.EffectMatrix);

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

            var effect = ValorAssets.OutlineEffect.Prepare(parameters =>
            {
                parameters["Texture1"].SetValue(ValorAssets.NoiseTexture.Value);
                parameters["EffectMatrix"].SetValue(Main.GameViewMatrix.EffectMatrix);
                parameters["ScreenSize"].SetValue(_renderTarget.Size);
                parameters["ScreenPosition"].SetValue(Main.screenPosition);
                parameters["OutlineColor"].SetValue(ValorProjectile.GlowColor.ToVector4());
                parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
            });

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect.Value, Main.GameViewMatrix.ZoomMatrix);
            Main.spriteBatch.Draw(_renderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            Main.spriteBatch.End();

            _targetWasPrepared = false;
        }

        private void DrawChains()
        {
            if (!_npcObserver.AnyEntity)
                return;

            Main.spriteBatch.End(out var spriteBatchSnapshot);
            Main.spriteBatch.Begin(spriteBatchSnapshot with { Effect = null });
            {
                /*var anchorTexture = ValorAssets.AnchorTexture;
                var anchorOrigin = anchorTexture.Size() * 0.5f;

                var segmentTexture = ValorAssets.ChainTexture;
                var segmentDefaultRectangle = new Rectangle(0, 0, 14, 16);
                var segmentGlowRectangle = new Rectangle(14, 0, 14, 16);
                var segmentOrigin = new Vector2(7, 8);

                foreach (var npc in _npcObserver.GetEntityInstances())
                {
                    if (!npc.TryGetGlobalNPC<ValorGlobalNPC>(out var valorNPC) || valorNPC.Data is null)
                        continue;

                    var chainData = valorNPC.Data;
                    var chainPoints = chainData.Physics.GetPositions().ToArray();

                    if (chainPoints.Length < 2)
                        continue;

                    var anchorColor = Lighting.GetColor(chainData.Tile);
                    var anchorPosition = chainData.Tile.ToWorldCoordinates() - Main.screenPosition;

                    Main.spriteBatch.Draw(anchorTexture.Value, anchorPosition, null, anchorColor, 0f, anchorOrigin, 1f, SpriteEffects.None, 0f);

                    for (int i = 0; i < chainPoints.Length; i++)
                    {
                        var segmentPoint = chainPoints[i];
                        var prevSegmentPoint = i == 0 ? chainPoints[1] : chainPoints[i - 1];
                        var segmentRotation = (segmentPoint - prevSegmentPoint).ToRotation() + MathHelper.PiOver2;
                        var lightColor = Lighting.GetColor(segmentPoint.ToTileCoordinates());

                        Main.spriteBatch.Draw(segmentTexture.Value, segmentPoint - Main.screenPosition, segmentDefaultRectangle, lightColor, segmentRotation, segmentOrigin, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.Draw(segmentTexture.Value, segmentPoint - Main.screenPosition, segmentGlowRectangle, Color.White * (i / (float)chainPoints.Length), segmentRotation, segmentOrigin, 1f, SpriteEffects.None, 0);
                    }
                }*/
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(spriteBatchSnapshot);
        }
    }
}