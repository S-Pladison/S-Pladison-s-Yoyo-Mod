using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Graphics.RenderTargets;
using SPYoyoMod.Common.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

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
            target.AddBuff(ModContent.BuffType<ValorBuff>(), ModUtils.SecondsToTicks(3f));
        }

        public override void PostDraw(Projectile proj, Color lightColor)
        {
            if (TileUtils.TryFindClosestTile(proj.Center.ToTileCoordinates(), (int)(ValorGlobalNPC.ChainLengthMax / TileUtils.TileSizeInPixels), i => WorldGen.SolidOrSlopedTile(i.X, i.Y) || Main.tile[i.X, i.Y].IsHalfBlock || TileID.Sets.Platforms[Main.tile[i.X, i.Y].TileType], out var tileCoord))
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
            ValorNPCOutlineHandler.AddNPC(npc);
        }

        void IDeletedFromNPCBuff.OnDeleteFromNPC(int buffType, int buffIndex, NPC npc)
        {
            ValorNPCOutlineHandler.RemoveNPC(npc);
            ValorGlobalNPC.DeactivateEffect(npc);
        }
    }

    public sealed class ValorGlobalNPC : GlobalNPC
    {
        public static readonly float ChainLengthMax = TileUtils.TileSizeInPixels * 7f;
        public static readonly int TileCheckFrequency = ModUtils.SecondsToTicks(1f);

        private int _timeSinceLastTileCheck;

        public override bool InstancePerEntity { get => true; }
        public bool IsChained { get; private set; }
        public bool MustBeChained { get; private set; }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (!CanBeChained(npc))
            {
                npc.buffImmune[ModContent.BuffType<ValorBuff>()] = true;
            }
        }

        public override bool PreAI(NPC npc)
        {
            // Периодически пытаемся приципить врага к плитке, если он еще не закреплен
            if (MustBeChained && !IsChained && _timeSinceLastTileCheck++ < TileCheckFrequency)
            {
                _timeSinceLastTileCheck = 0;

                ChainToTile(npc);
            }

            return true;
        }

        public static void ActivateEffect(NPC npc)
        {
            var globalNPC = npc.GetGlobalNPC<ValorGlobalNPC>();

            if (globalNPC.MustBeChained)
                return;

            globalNPC.MustBeChained = true;
            npc.netUpdate = true;
        }

        public static void DeactivateEffect(NPC npc)
        {
            var globalNPC = npc.GetGlobalNPC<ValorGlobalNPC>();

            if (!globalNPC.MustBeChained)
                return;

            globalNPC.BreakChain(npc);
            globalNPC.MustBeChained = false;
            npc.netUpdate = true;
        }

        private bool ChainToTile(NPC npc)
        {
            if (IsChained)
                return false;

            if (!TryFindSuitableTile(npc, out var tileCoord))
                return false;

            // ...

            IsChained = true;
            npc.netUpdate = true;
            return true;
        }

        private bool BreakChain(NPC npc)
        {
            if (!IsChained)
                return false;

            // ...

            IsChained = false;
            npc.netUpdate = true;
            return true;
        }

        private static bool CanBeChained(NPC npc)
            => npc.CanBeChasedBy() && !npc.IsBossOrRelated();

        private static bool TryFindSuitableTile(NPC npc, out Point tileCoord)
            => TileUtils.TryFindTileSpiralTraverse(
                centerCoord: npc.Center.ToTileCoordinates(),
                tilesFromCenter: (int)(ChainLengthMax / TileUtils.TileSizeInPixels),
                predicate: tileCoord => true,
                tileCoord: out tileCoord);
    }

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
            => ModContent.GetInstance<ValorNPCOutlineHandler>()._npcObserver.Add(npc);

        public static void RemoveNPC(NPC npc)
            => ModContent.GetInstance<ValorNPCOutlineHandler>()._npcObserver.Remove(npc);

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