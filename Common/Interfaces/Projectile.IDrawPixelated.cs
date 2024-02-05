using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    /// <summary>
    ///  This interface allows you to draw pixelated things for all projectiles, including vanilla projectiles.
    /// </summary>
    public interface IDrawPixelatedProjectile : IPreDrawPixelatedProjectile, IPostDrawPixelatedProjectile { }

    /// <summary>
    ///  This interface allows you to draw pixelated things behind all projectiles, including vanilla projectiles.
    /// </summary>
    public interface IPreDrawPixelatedProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IPreDrawPixelatedProjectile).GetMethod(nameof(PreDrawPixelated)))
            );

        /// <summary>
        /// Allows you to draw pixelated things behind a projectile. Use the <see cref="ProjectileDrawLayers.PixelatedPrimitiveMatrices"/>
        /// for drawing primitives. Primitives will be drawn before sprites.
        /// </summary>
        void PreDrawPixelated(Projectile proj);

        public static int FirstProjIndex(IReadOnlyList<Projectile> projectiles)
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IPreDrawPixelatedProjectile)
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

                if (proj.ModProjectile is IPreDrawPixelatedProjectile m)
                    m.PreDrawPixelated(proj);

                foreach (IPreDrawPixelatedProjectile g in Hook.Enumerate(proj))
                    g.PreDrawPixelated(proj);
            }
        }
    }

    /// <summary>
    ///  This interface allows you to draw pixelated things in front of all projectiles, including vanilla projectiles.
    /// </summary>
    public interface IPostDrawPixelatedProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IPostDrawPixelatedProjectile).GetMethod(nameof(PostDrawPixelated)))
            );

        /// <summary>
        /// Allows you to draw pixelated things in front of a projectile. Use the <see cref="ProjectileDrawLayers.PixelatedPrimitiveMatrices"/>
        /// for drawing primitives. Primitives will be drawn before sprites.
        /// </summary>
        void PostDrawPixelated(Projectile proj);

        public static int FirstProjIndex(IReadOnlyList<Projectile> projectiles)
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IPostDrawPixelatedProjectile)
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

                if (proj.ModProjectile is IPostDrawPixelatedProjectile m)
                    m.PostDrawPixelated(proj);

                foreach (IPostDrawPixelatedProjectile g in Hook.Enumerate(proj))
                    g.PostDrawPixelated(proj);
            }
        }
    }
}