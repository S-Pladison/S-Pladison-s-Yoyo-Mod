using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using SPYoyoMod.Common.Graphics.RenderTargets;
using SPYoyoMod.Common.Hooks;
using SPYoyoMod.Utils;
using System;
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
            target.AddBuff(ModContent.BuffType<ValorBuff>(), ModUtils.SecondsToTicks(7f));
        }

        public override void PostDraw(Projectile proj, Color lightColor)
        {
            if (TileUtils.TryFindClosestTile(proj.Center.ToTileCoordinates(), ValorGlobalNPC.TileCheckRadius, i => WorldGen.SolidOrSlopedTile(i.X, i.Y) || Main.tile[i.X, i.Y].IsHalfBlock || TileID.Sets.Platforms[Main.tile[i.X, i.Y].TileType], out var tileCoord))
            {
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, tileCoord.ToWorldCoordinates(0, 0) - Main.screenPosition, new Rectangle(0, 0, 16, 16), Color.Red);
            }
        }
    }

    public sealed class ValorBuff : ModBuff, IAddedToNPCBuff, IDeletedFromNPCBuff
    {
        public override string Texture => ValorAssets.BuffPath;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
        }

        void IAddedToNPCBuff.OnAddToNPC(int buffType, int buffIndex, NPC npc)
        {
            ValorGlobalNPC.ActivateEffect(npc);
        }

        void IDeletedFromNPCBuff.OnDeleteFromNPC(int buffType, int buffIndex, NPC npc)
        {
            ValorGlobalNPC.DeactivateEffect(npc);
        }
    }

    public sealed class ValorGlobalNPC : GlobalNPC
    {
        public static readonly int TileCheckFrequency = ModUtils.SecondsToTicks(1f);
        public static readonly int TileCheckRadius = 7;
        public static readonly float ChainAddLength = TileUtils.TileSizeInPixels * 2.5f;
        public static readonly float ChainLengthToBreak = TileUtils.TileSizeInPixels * 12f;

        private int _timeSinceLastTileCheck;
        private Point? _chainTileCoord;
        private float _chainMaxLength;

        public override bool InstancePerEntity { get => true; }
        public bool IsChained { get => _chainTileCoord is not null; }
        public bool MustBeChained { get; private set; }

        public override void Load()
        {
            // Из-за мелких артифактов по типу тряски и т.п., решил что лучше решения не будет
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
            if (!MustBeChained)
                return true;

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

            // Разрушаем цепь, если НПС слишком далеко от тайла
            if (Vector2.Distance(_chainTileCoord.Value.ToWorldCoordinates(), npc.Center) >= ChainLengthToBreak)
            {
                BreakChain(npc);
                return true;
            }

            // Разрушаем цепь для Goblin Sorcerer, Tim, Dark Caster и других перед телепортацией
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

            binaryWriter.Write((short)_chainTileCoord.Value.X);
            binaryWriter.Write((short)_chainTileCoord.Value.Y);
            binaryWriter.Write(_chainMaxLength);
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

            _chainTileCoord = new Point(binaryReader.ReadInt16(), binaryReader.ReadInt16());
            _chainMaxLength = binaryReader.ReadSingle();
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (_chainTileCoord is null)
                return;

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, _chainTileCoord.Value.ToWorldCoordinates(0, 0) - Main.screenPosition, new Rectangle(0, 0, 16, 16), Color.Blue);
        }

        public static void ActivateEffect(NPC npc)
        {
            var globalNPC = npc.GetGlobalNPC<ValorGlobalNPC>();

            if (globalNPC.MustBeChained)
                return;

            globalNPC.MustBeChained = true;
            globalNPC.ChainToTile(npc);
            npc.netUpdate = true;

            ValorNPCOutlineHandler.AddNPC(npc);
        }

        public static void DeactivateEffect(NPC npc)
        {
            var globalNPC = npc.GetGlobalNPC<ValorGlobalNPC>();

            if (!globalNPC.MustBeChained)
                return;

            globalNPC.BreakChain(npc);
            globalNPC.MustBeChained = false;
            npc.netUpdate = true;

            ValorNPCOutlineHandler.RemoveNPC(npc);
        }

        private bool ChainToTile(NPC npc)
        {
            if (IsChained)
                return false;

            if (!TryFindSuitableTile(npc, out var tileCoord))
                return false;

            var distFromNPCToTile = Vector2.Distance(tileCoord.ToWorldCoordinates(), npc.Center);

            if (distFromNPCToTile >= ChainLengthToBreak)
                return false;

            _chainTileCoord = tileCoord;
            _chainMaxLength = distFromNPCToTile + ChainAddLength;
            npc.netUpdate = true;
            return true;
        }

        private bool BreakChain(NPC npc)
        {
            if (!IsChained)
                return false;

            _chainMaxLength = 0;
            _chainTileCoord = null;
            npc.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Unlock, npc.Center);

            return true;
        }

        private void UpdateCollision(NPC npc)
        {
            var chainPosition = _chainTileCoord.Value.ToWorldCoordinates();

            var nextPosition = npc.Center + npc.velocity;
            var vectorFromChainToNPC = nextPosition - chainPosition;
            var vectorFromChainToNPCLength = vectorFromChainToNPC.Length();

            if (vectorFromChainToNPCLength <= _chainMaxLength)
                return;

            var normalizedVectorFromChainToNPC = Vector2.Normalize(vectorFromChainToNPC);
            var newPosition = chainPosition + normalizedVectorFromChainToNPC * _chainMaxLength;
            var velocityCorrection = newPosition - nextPosition;

            npc.velocity += velocityCorrection;
        }

        private static bool CanBeChained(NPC npc)
            => npc.CanBeChasedBy() && !npc.IsBossOrRelated();

        private static bool TryFindSuitableTile(NPC npc, out Point tileCoord)
            => TileUtils.TryFindClosestTile(
                centerCoord: npc.Center.ToTileCoordinates(),
                tilesFromCenter: TileCheckRadius,
                predicate: t => WorldGen.SolidOrSlopedTile(t.X, t.Y) || Main.tile[t.X, t.Y].IsHalfBlock || TileID.Sets.Platforms[Main.tile[t.X, t.Y].TileType],
                tileCoord: out tileCoord);
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class ValorNPCOutlineHandler : ILoadable
    {
        private readonly ScreenRenderTarget _renderTarget = ScreenRenderTarget.Create(ScreenRenderTargetScale.Default);
        private readonly NPCObserver _npcObserver = new(n => !n.TryGetGlobalNPC(out ValorGlobalNPC valorNPC) || !valorNPC.MustBeChained);

        private bool _targetWasPrepared = false;

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            ModEvents.OnPostUpdateEverything += _npcObserver.Update;
            ModEvents.OnPostUpdateCameraPosition += DrawToTarget;

            On_Main.DoDraw_Tiles_NonSolid += (orig, main) =>
            {
                orig(main);
                DrawToScreen();
            };
        }

        void ILoadable.Unload()
        {
            ModEvents.OnPostUpdateCameraPosition -= DrawToTarget;
            ModEvents.OnPostUpdateEverything -= _npcObserver.Update;
        }

        public static void AddNPC(NPC npc)
            => ModContent.GetInstance<ValorNPCOutlineHandler>()?._npcObserver.Add(npc);

        public static void RemoveNPC(NPC npc)
            => ModContent.GetInstance<ValorNPCOutlineHandler>()?._npcObserver.Remove(npc);

        private void DrawToTarget()
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

        private void DrawToScreen()
        {
            if (!_targetWasPrepared)
                return;

            var effect = ValorAssets.NPCOutlineEffect.Prepare(parameters =>
            {
                parameters["ScreenSize"].SetValue(_renderTarget.Size);
                parameters["OutlineColor"].SetValue(new Color(35, 90, 255).ToVector4());
                parameters["Zoom"].SetValue(new Vector2(Main.GameZoomTarget));
            });

            Main.spriteBatch.End(out var spriteBatchSnapshot);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect.Value, Matrix.Identity);
            Main.spriteBatch.Draw(_renderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(spriteBatchSnapshot);

            _targetWasPrepared = false;
        }
    }
}