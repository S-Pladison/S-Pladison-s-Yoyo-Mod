using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    /// <summary>
    /// This interface allows you to draw pixelated things with additive blend state for all projectiles, including vanilla projectiles.
    /// </summary>
    public interface IDrawPixelatedAdditiveProjectile : IPreDrawPixelatedAdditiveProjectile, IPostDrawPixelatedAdditiveProjectile { }

    /// <summary>
    /// This interface allows you to draw pixelated things with additive blend state behind all projectiles, including vanilla projectiles.
    /// </summary>
    public interface IPreDrawPixelatedAdditiveProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IPreDrawPixelatedAdditiveProjectile).GetMethod(nameof(PreDrawPixelatedAdditive)))
            );

        /// <summary>
        /// Allows you to draw pixelated things with additive blend state behind a projectile. Use the <see cref="ProjectileDrawLayers.PixelatedPrimitiveMatrices"/>
        /// for drawing primitives. Primitives will be drawn before sprites.
        /// </summary>
        void PreDrawPixelatedAdditive(Projectile proj);
    }

    /// <summary>
    /// This interface allows you to draw pixelated things with additive blend state in front of all projectiles, including vanilla projectiles.
    /// </summary>
    public interface IPostDrawPixelatedAdditiveProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IPostDrawPixelatedAdditiveProjectile).GetMethod(nameof(PostDrawPixelatedAdditive)))
            );

        /// <summary>
        /// Allows you to draw pixelated things with additive blend state in front of a projectile. Use the <see cref="ProjectileDrawLayers.PixelatedPrimitiveMatrices"/>
        /// for drawing primitives. Primitives will be drawn before sprites.
        /// </summary>
        void PostDrawPixelatedAdditive(Projectile proj);
    }
}