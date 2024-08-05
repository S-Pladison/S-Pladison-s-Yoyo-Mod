using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Hooks;
using SPYoyoMod.Common.ModSupport;
using SPYoyoMod.Utils;
using Terraria;
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
            if (!npc.TryGetGlobalNPC<ValorGlobalNPC>(out var globalNPC))
            {
                npc.DelBuff(buffIndex);
                return;
            }

            if (!globalNPC.TryFindSuitableTile(npc, out var tileCoord))
            {
                npc.DelBuff(buffIndex);
                return;
            }

            globalNPC.ChainToTile(npc, tileCoord);
        }

        void IDeletedFromNPCBuff.OnDeleteFromNPC(int buffType, int buffIndex, NPC npc)
        {
            npc.GetGlobalNPC<ValorGlobalNPC>().BreakChain(npc);
        }
    }

    public sealed class ValorGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public bool IsChained { get; private set; }

        public bool TryFindSuitableTile(NPC npc, out Point tileCoord)
        {
            tileCoord = default;

            return true;
        }

        public void ChainToTile(NPC npc, Point tileCoord)
        {
            if (IsChained)
                return;

            // ...

            IsChained = true;
            npc.netUpdate = true;
        }

        public void BreakChain(NPC npc)
        {
            if (!IsChained)
                return;

            // ...

            IsChained = false;
            npc.netUpdate = true;
        }
    }

    public sealed class ValorPlayer : ModPlayer
    {
        private int _chainedNPCIndex = -1;
    }
}