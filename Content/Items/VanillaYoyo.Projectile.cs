using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Configs;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items
{
    public abstract class VanillaYoyoProjectile : GlobalProjectile, IModifyYoyoStatsProjectile, IPostDrawYoyoStringProjectile
    {
        /// <summary>
        /// The Projectile ID of this yoyo. The Projectile ID is a unique number assigned
        /// to each Projectile loaded into the game.
        /// </summary>
        public abstract int YoyoType { get; }
        public override bool InstancePerEntity { get => true; }

        public sealed override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.type < ProjectileID.Count && entity.type.Equals(YoyoType);
        }

        public sealed override bool IsLoadingEnabled(Terraria.ModLoader.Mod mod)
        {
            return ModContent.GetInstance<ServerSideConfig>().ReworkedVanillaYoyos;
        }

        /// <inheritdoc cref="IModifyYoyoStatsProjectile.ModifyYoyoStats(Projectile, ref YoyoStatModifiers)" />
        public virtual void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers) { }

        /// <inheritdoc cref="IPostDrawYoyoStringProjectile.PostDrawYoyoString(Projectile, Vector2)" />
        public virtual void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter) { }
    }
}