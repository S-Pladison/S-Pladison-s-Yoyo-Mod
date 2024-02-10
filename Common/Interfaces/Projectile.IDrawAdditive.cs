using System.Collections.Generic;
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

        public static int FirstProjIndex(IReadOnlyList<Projectile> projectiles)
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IPreDrawAdditiveProjectile)
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

                if (proj.ModProjectile is IPreDrawAdditiveProjectile m)
                    m.PreDrawAdditive(proj);

                foreach (IPreDrawAdditiveProjectile g in Hook.Enumerate(proj))
                    g.PreDrawAdditive(proj);
            }
        }
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

        public static int FirstProjIndex(IReadOnlyList<Projectile> projectiles)
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IPostDrawAdditiveProjectile)
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

                if (proj.ModProjectile is IPostDrawAdditiveProjectile m)
                    m.PostDrawAdditive(proj);

                foreach (IPostDrawAdditiveProjectile g in Hook.Enumerate(proj))
                    g.PostDrawAdditive(proj);
            }
        }
    }
}