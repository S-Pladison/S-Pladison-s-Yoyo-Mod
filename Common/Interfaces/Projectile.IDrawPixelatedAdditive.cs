using System.Collections.Generic;
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

        public static int FirstProjIndex(IReadOnlyList<Projectile> projectiles)
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IPreDrawPixelatedAdditiveProjectile)
                    return i;

                foreach (var _ in Hook.Enumerate(proj))
                    return i;
            }

            return -1;
        }

        public static void DrawProjs(IReadOnlyList<Projectile> projectiles, int startIndex)
        {
            for (int i = startIndex; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IPreDrawPixelatedAdditiveProjectile m)
                    m.PreDrawPixelatedAdditive(proj);

                foreach (IPreDrawPixelatedAdditiveProjectile g in Hook.Enumerate(proj))
                    g.PreDrawPixelatedAdditive(proj);
            }
        }
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

        public static int FirstProjIndex(IReadOnlyList<Projectile> projectiles)
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IPostDrawPixelatedAdditiveProjectile)
                    return i;

                foreach (var _ in Hook.Enumerate(proj))
                    return i;
            }

            return -1;
        }

        public static void DrawProjs(IReadOnlyList<Projectile> projectiles, int startIndex)
        {
            for (int i = startIndex; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IPostDrawPixelatedAdditiveProjectile m)
                    m.PostDrawPixelatedAdditive(proj);

                foreach (IPostDrawPixelatedAdditiveProjectile g in Hook.Enumerate(proj))
                    g.PostDrawPixelatedAdditive(proj);
            }
        }
    }
}