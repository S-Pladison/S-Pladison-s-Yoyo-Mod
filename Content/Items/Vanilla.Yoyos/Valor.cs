using Microsoft.Xna.Framework;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class ValorAssets : ILoadable
    {
        // [ Общее ]
        private const string _path = $"{nameof(SPYoyoMod)}/Assets/Items/Vanilla.Yoyos/Valor/";

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
            if (!target.TryGetGlobalNPC<ValorGlobalNPC>(out var globalNPC))
                return;

            globalNPC.ChainToTile(target);
        }
    }

    public sealed class ValorGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public void ChainToTile(NPC npc)
        {
            if (!npc.CanBeChasedBy() || npc.IsBossOrRelated())
                return;

            if (!TryFindSuitableTile(npc, out var tileCoord))
                return;

            // ...
        }

        private bool TryFindSuitableTile(NPC npc, out Point tileCoord)
        {
            tileCoord = default;

            return true;
        }
    }
}