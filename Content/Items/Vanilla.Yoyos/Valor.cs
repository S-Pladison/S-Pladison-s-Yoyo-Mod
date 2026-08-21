using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Graphics.RenderTargets;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Core.Netcode;
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

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    // https://www.artstation.com/artwork/DLbnDG

    public sealed class ValorAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Valor/Valor";

        public const string InvisiblePath = $"{AssetPath}/Invisible";
        public const string StringPath = $"{AssetPath}/FishingLine_WithShadow";

        public static readonly LazyAsset<Texture2D> GlowTexture = LazyAsset<Texture2D>.From($"{AssetPath}/YoyoGlow_WithShadow");
        public static readonly LazyAsset<Texture2D> NoiseTexture = LazyAsset<Texture2D>.From($"{AssetPath}/CloudNoise");
        public static readonly LazyAsset<Texture2D> AnchorTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Anchor");
        public static readonly LazyAsset<Texture2D> ChainTexture = LazyAsset<Texture2D>.From($"{YoyoPath}_Chain");
        public static readonly LazyAsset<Effect> TrailEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Trail");
        public static readonly LazyAsset<Effect> OutlineEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Outline");
    }

    public sealed class ValorItem : VanillaYoyoBaseItem
    {
        public static readonly int DebuffApplyChanceDenominator = 1; //< TODO: Тут была 9
        public static readonly float DebuffChanceReductionDistance = MathF.Pow(TileUtils.TileSizeInPixels * 12f, 2f); //< Возводим в степень из-за использования DistanceSquared

        public override int ItemType => ItemID.Valor;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Math.Ceiling(1.0f / DebuffApplyChanceDenominator * 100.0f));

        public override void SetDefaults(Item item)
        {
            item.knockBack = 4.5f;
        }
    }

    public sealed class ValorProjectile : VanillaYoyoBaseProjectile, IInitializableProjectile, IPreDrawPixelatedProjectile, IEmitLightEntity
    {
        public static readonly Color GlowColor = new(35, 90, 255);
        public static readonly int TrailPointCount = 7;

        private YoyoStringRenderer _stringRenderer;
        private StripRenderer _trailRenderer;
        private LinkedList<Vector2> _oldPositions;

        public override int ProjType => ProjectileID.Valor;
        public override bool InstancePerEntity => true;

        void IInitializableProjectile.Initialize(Projectile _)
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

        public override void OnKill(Projectile projectile, int timeLeft)
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
            for (int i = 0; i < 5; i++)
            {
                var dust = Main.dust[Dust.NewDust(proj.position, proj.width, proj.height, Main.rand.NextBool() ? DustID.DungeonWater : DustID.WaterCandle)];
                dust.noGravity = true;
                dust.noLightEmittence = true;
                dust.velocity = Vector2.Normalize(proj.Center - target.Center).RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * Main.rand.NextFloat(1.5f, 4.0f);
            }

            if (!Main.rand.NextBool(ValorItem.DebuffApplyChanceDenominator))
                return;

            foreach (var npc in Main.ActiveNPCs)
            {
                if (npc.whoAmI == target.whoAmI)
                    continue;

                if (Vector2.DistanceSquared(npc.Center, target.Center) > ValorItem.DebuffChanceReductionDistance)
                    continue;

                if (npc.TryGetGlobalNPC<ValorGlobalNPC>(out var valorNpc) && valorNpc.IsChained)
                    return;
            }

            if (target.TryGetGlobalNPC<ValorGlobalNPC>(out var valorTarget))
                valorTarget.TryApplyChain(target);
        }

        void IEmitLightEntity.EmitLight(Entity proj)
        {
            Lighting.AddLight(proj.Center, GlowColor.ToVector3() * 0.2f);
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

            if (!proj.TryGetOwner(out var owner))
                return;

            var settings = new YoyoStringRendererSettings(
                proj: proj,
                start: mountedCenter + owner.gfxOffY * Vector2.UnitY,
                offset: -Main.screenPosition
            );

            _stringRenderer.Render(Main.spriteBatch, settings);
        }
    }

    public sealed class ValorGlobalNPC : GlobalNPC, IEmitLightEntity
    {
        public sealed class ChainData
        {
            public const float SegmentLength = 8f;
            public const int SolverIterations = 5;

            public Tile Tile { get => Main.tile[Position.X, Position.Y]; }

            public readonly NPC Target;
            public readonly float Length;
            public readonly PhysicalChain Physics;

            public Point Position;
            public ushort LifeTime;

            public ChainData(Point start, NPC target, float length, ushort lifeTime = 0)
            {
                length = (length / (int)SegmentLength) * SegmentLength;
                length = MathF.Max(length, SegmentLength);

                Position = start;
                Length = length;
                LifeTime = lifeTime;
                Target = target;

                var worldPos = start.ToWorldCoordinates();
                var directionToNPC = Terraria.Utils.SafeNormalize(target.Center - worldPos, Vector2.Zero);

                Physics = PhysicalChain.CreateTautBetween(worldPos, worldPos + directionToNPC * length, SegmentLength);
            }

            public void Update()
            {
                if ((Target.whoAmI + Main.GameUpdateCount) % 2 == 0)
                {
                    var tilePos = Position.ToWorldCoordinates();
                    var slack = 1.0f - Vector2.Distance(Target.Center, tilePos) / Length;

                    Physics.Gravity = new Vector2(0f, MathHelper.Max(slack, 0f));
                    Physics.Simulate(tilePos, Target.Center, SolverIterations);
                }

                if (LifeTime++ >= GeneralUtils.SecondsToTicks(7f) && Target.TryGetGlobalNPC<ValorGlobalNPC>(out var valorTarget))
                    valorTarget.BreakChain(Target);
            }
        }

        public static readonly int TileCheckFrequency = GeneralUtils.SecondsToTicks(1f);
        public static readonly int TileCheckRadius = 7;
        public static readonly float ChainAdditionalLength = TileUtils.TileSizeInPixels * 2.5f;
        public static readonly float ChainLengthToBreak = TileUtils.TileSizeInPixels * 12f;

        private int _timeSinceLastTileCheck;

        public override bool InstancePerEntity { get => true; }
        public bool IsChained { get; private set; }
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

        public bool TryApplyChain(NPC npc)
        {
            if (IsChained || !CanBeChained(npc))
                return false;

            return ChainToTile(npc);
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
            //UpdateDebuffState(npc, npc.HasBuff<ValorBuff>());

            if (!IsChained)
                return true;

            if (Main.rand.NextBool(4))
            {
                var dust = Main.dust[Dust.NewDust(npc.position, npc.width, npc.height, Main.rand.NextBool() ? DustID.DungeonWater : DustID.WaterCandle)];
                dust.noGravity = true;
                dust.noLightEmittence = true;
            }

            // Обновляем физику цепи и счетчик жизни
            Data?.Update();

            HandleChain(npc);

            return true;
        }

        private void UpdateDebuffState(NPC npc, bool state)
        {
            if (IsChained == state)
                return;

            IsChained = state;

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
            if (Vector2.Distance(Data.Position.ToWorldCoordinates(), npc.Center) >= ChainLengthToBreak)
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
                    SoundEngine.PlaySound(SoundID.Unlock, npc.Center);
                    Data = updatedData;
                    IsChained = true;
                    ModContent.GetInstance<ValorNPCOutlineEffectHandler>()?.Add(npc);
                    return;
                }

                Data.Position = updatedData.Position;
                //Data.Length = updatedData.Length;
                Data.LifeTime = updatedData.LifeTime;
                IsChained = true;
                ModContent.GetInstance<ValorNPCOutlineEffectHandler>()?.Add(npc);
                return;
            }

            ModContent.GetInstance<ValorNPCOutlineEffectHandler>()?.Remove(npc);

            if (Data is not null)
            {
                SoundEngine.PlaySound(SoundID.Unlock, npc.Center);
                _timeSinceLastTileCheck = TileCheckFrequency;
                Data = null;
                IsChained = false;
                return;
            }

            Data = null;
            IsChained = false;
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

            npc.netUpdate = true;
            return true;
        }

        private void UpdateCollision(NPC npc)
        {
            if (Data is null)
                return;

            var chainPosition = Data.Position.ToWorldCoordinates();

            var nextPosition = npc.Center + npc.velocity;
            var vectorFromChainToNPC = nextPosition - chainPosition;
            var vectorFromChainToNPCLength = vectorFromChainToNPC.Length();

            if (vectorFromChainToNPCLength <= Data.Length)
                return;

            var normalizedVectorFromChainToNPC = Terraria.Utils.SafeNormalize(vectorFromChainToNPC, Vector2.Zero);
            var newPosition = chainPosition + normalizedVectorFromChainToNPC * Data.Length;
            var velocityCorrection = newPosition - nextPosition;

            npc.velocity += velocityCorrection;
        }

        void IEmitLightEntity.EmitLight(Entity npc)
        {
            if (!IsChained)
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
        private readonly NPCObserver _npcObserver = NPCObserver.Create(n => !n.TryGetGlobalNPC(out ValorGlobalNPC valorNPC) || !valorNPC.IsChained);

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
                var anchorTexture = ValorAssets.AnchorTexture.Value;
                var anchorOrigin = anchorTexture.Size() * 0.5f;

                var segmentTexture = ValorAssets.ChainTexture.Value;
                var segmentDefaultRectangle = new Rectangle(0, 0, 14, 16);
                var segmentGlowRectangle = new Rectangle(14, 0, 14, 16);
                var segmentOrigin = new Vector2(7, 8);

                foreach (var npc in _npcObserver.GetEntityInstances())
                {
                    if (!npc.TryGetGlobalNPC<ValorGlobalNPC>(out var valorNPC) || valorNPC.Data is null)
                        continue;

                    var chainData = valorNPC.Data;
                    var chain = chainData.Physics;

                    if (chain.NodeCount < 2)
                        continue;

                    var anchorColor = Lighting.GetColor(chainData.Position);
                    var anchorPosition = chainData.Position.ToWorldCoordinates() - Main.screenPosition;

                    Main.spriteBatch.Draw(anchorTexture, anchorPosition, null, anchorColor, 0f, anchorOrigin, 1f, SpriteEffects.None, 0f);

                    for (int i = 0; i < chain.NodeCount; i++)
                    {
                        var segmentPoint = chain[i].Position;
                        var prevSegmentPoint = i == 0 ? chain[1].Position : chain[i - 1].Position;
                        var segmentRotation = (segmentPoint - prevSegmentPoint).ToRotation() + MathHelper.PiOver2;
                        var lightColor = Lighting.GetColor(segmentPoint.ToTileCoordinates());

                        Main.spriteBatch.Draw(segmentTexture, segmentPoint - Main.screenPosition, segmentDefaultRectangle, lightColor, segmentRotation, segmentOrigin, 1f, SpriteEffects.None, 0);
                        Main.spriteBatch.Draw(segmentTexture, segmentPoint - Main.screenPosition, segmentGlowRectangle, Color.White * (i / (float)chain.NodeCount), segmentRotation, segmentOrigin, 1f, SpriteEffects.None, 0);
                    }
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(spriteBatchSnapshot);
        }
    }
}