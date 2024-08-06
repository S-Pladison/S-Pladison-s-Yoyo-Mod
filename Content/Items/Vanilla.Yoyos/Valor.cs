using Microsoft.Xna.Framework;
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
        }

        void IDeletedFromNPCBuff.OnDeleteFromNPC(int buffType, int buffIndex, NPC npc)
        {
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
            if (!CheckCanBeChained(npc))
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

        private static bool CheckCanBeChained(NPC npc)
            => !npc.IsBossOrRelated();

        private static bool TryFindSuitableTile(NPC npc, out Point tileCoord)
            => TileUtils.TryFindTileSpiralTraverse(
                centerCoord: npc.Center.ToTileCoordinates(),
                tilesFromCenter: (int)(ChainLengthMax / TileUtils.TileSizeInPixels),
                predicate: tileCoord => true,
                tileCoord: out tileCoord);
    }

    public sealed class ValorPlayer : ModPlayer
    {
        private int _chainedNPCIndex = -1;
    }
}