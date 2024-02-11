using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    /// <summary>
    /// This interface allows you to draw things with additive blend state for all projectiles, including vanilla projectiles.
    /// </summary>
    public interface IDrawAdditiveProjectile : IPreDrawAdditiveProjectile, IPostDrawAdditiveProjectile { }

    /// <summary>
    /// This interface allows you to draw things with additive blend state behind all projectiles, including vanilla projectiles.
    /// </summary>
    public interface IPreDrawAdditiveProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IPreDrawAdditiveProjectile).GetMethod(nameof(PreDrawAdditive)))
            );

        /// <summary>
        /// Allows you to draw things with additive blend state behind a projectile. Use the <see cref="ProjectileDrawLayers.DefaultPrimitiveMatrices"/>
        /// for drawing primitives. Primitives will be drawn before sprites.
        /// </summary>
        void PreDrawAdditive(Projectile proj);
    }

    /// <summary>
    /// This interface allows you to draw things with additive blend state in front of all projectiles, including vanilla projectiles.
    /// </summary>
    public interface IPostDrawAdditiveProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IPostDrawAdditiveProjectile).GetMethod(nameof(PostDrawAdditive)))
            );

        /// <summary>
        /// Allows you to draw things with additive blend state in front of a projectile. Use the <see cref="ProjectileDrawLayers.DefaultPrimitiveMatrices"/>
        /// for drawing primitives. Primitives will be drawn before sprites.
        /// </summary>
        void PostDrawAdditive(Projectile proj);
    }
}