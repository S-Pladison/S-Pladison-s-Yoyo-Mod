using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Configs;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Utils.DataStructures;
using SPYoyoMod.Utils.Extensions;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items
{
    public abstract class VanillaYoyoProjectile : GlobalProjectile, IModifyYoyoStatsProjectile, IPostDrawYoyoStringProjectile
    {
        private readonly int yoyoType;

        public VanillaYoyoProjectile(int yoyoType)
        {
            this.yoyoType = yoyoType;
        }

        public sealed override bool AppliesToEntity(Projectile entity, bool lateInstantiation) { return entity.type < ProjectileID.Count && entity.IsYoyo() && entity.type.Equals(yoyoType); }
        public sealed override bool IsLoadingEnabled(Terraria.ModLoader.Mod mod) { return ModContent.GetInstance<ServerSideConfig>().ReworkedVanillaYoyos; }

        public virtual void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers) { }
        public virtual void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter) { }
    }
}