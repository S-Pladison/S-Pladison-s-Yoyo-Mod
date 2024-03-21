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

        /// <summary>
        /// How long in seconds the yoyo will stay out before automatically returning to the player.
        /// Leaving as -1 will make the time infinite.
        /// Default value is null, which applies vanilla value.
        /// </summary>
        public virtual float? LifeTime { get; }

        /// <summary>
        /// The maximum distance a yoyo projectile can be from its owner in pixels.
        /// Default value is null, which applies vanilla value.
        /// </summary>
        public virtual float? MaxRange { get; }

        /// <summary>
        /// The maximum speed a yoyo projectile can go in pixels per tick.
        /// Default value is null, which applies vanilla value.
        /// </summary>
        public virtual float? TopSpeed { get; }

        public override bool InstancePerEntity { get => true; }

        public sealed override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.type < ProjectileID.Count && entity.type.Equals(YoyoType);
        }

        public sealed override void SetStaticDefaults()
        {
            if (LifeTime.HasValue)
            {
                ProjectileID.Sets.YoyosLifeTimeMultiplier[YoyoType] = LifeTime.Value;
            }

            if (MaxRange.HasValue)
            {
                ProjectileID.Sets.YoyosMaximumRange[YoyoType] = MaxRange.Value;
            }

            if (TopSpeed.HasValue)
            {
                ProjectileID.Sets.YoyosTopSpeed[YoyoType] = TopSpeed.Value;
            }

            YoyoSetStaticDefaults();
        }

        public sealed override bool IsLoadingEnabled(Terraria.ModLoader.Mod mod)
        {
            return ModContent.GetInstance<ServerSideConfig>().ReworkedVanillaYoyos;
        }

        /// <inheritdoc cref="SetStaticDefaults" />
        public virtual void YoyoSetStaticDefaults() { }

        /// <inheritdoc cref="IModifyYoyoStatsProjectile.ModifyYoyoStats(Projectile, ref YoyoStatModifiers)" />
        public virtual void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers) { }

        /// <inheritdoc cref="IPostDrawYoyoStringProjectile.PostDrawYoyoString(Projectile, Vector2)" />
        public virtual void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter) { }
    }
}