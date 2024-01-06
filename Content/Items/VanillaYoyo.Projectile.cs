using Microsoft.Xna.Framework;
using SPYoyoMod.Common;
using SPYoyoMod.Common.Configs;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items
{
    public abstract class VanillaYoyoProjectile : GlobalProjectile, IModifyYoyoStats, IPostDrawYoyoString
    {
        private readonly int yoyoType;

        public VanillaYoyoProjectile(int yoyoType)
        {
            this.yoyoType = yoyoType;
        }

        public sealed override bool AppliesToEntity(Projectile entity, bool lateInstantiation) { return entity.type.Equals(yoyoType); }
        public sealed override bool IsLoadingEnabled(Terraria.ModLoader.Mod mod) { return ModContent.GetInstance<ServerSideConfig>().ReworkedVanillaYoyos; }

        public virtual void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers) { }
        public virtual void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter) { }
    }
}