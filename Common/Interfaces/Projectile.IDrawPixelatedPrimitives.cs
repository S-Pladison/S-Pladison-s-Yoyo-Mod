using SPYoyoMod.Utils.DataStructures;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    public interface IDrawPixelatedPrimitivesProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> PreHook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IDrawPixelatedPrimitivesProjectile).GetMethod(nameof(PreDrawPixelatedPrimitives)))
            );

        public static readonly GlobalHookList<GlobalProjectile> PostHook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IDrawPixelatedPrimitivesProjectile).GetMethod(nameof(PostDrawPixelatedPrimitives)))
            );

        void PreDrawPixelatedPrimitives(Projectile proj, PrimitiveMatrices matrices) { }
        void PostDrawPixelatedPrimitives(Projectile proj, PrimitiveMatrices matrices) { }

        public static int FirstProjIndex(IReadOnlyList<Projectile> projectiles, bool preDraw)
        {
            var hook = preDraw ? PreHook : PostHook;

            for (int i = 0; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IDrawPixelatedPrimitivesProjectile)
                    return i;

                foreach (var _ in hook.Enumerate(proj))
                    return i;
            }

            return -1;
        }

        public static void PreDrawProjs(IReadOnlyList<Projectile> projectiles, int startIndex, PrimitiveMatrices matrices)
        {
            for (int i = startIndex; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IDrawPixelatedPrimitivesProjectile m)
                    m.PreDrawPixelatedPrimitives(proj, matrices);

                foreach (IDrawPixelatedPrimitivesProjectile g in PreHook.Enumerate(proj))
                    g.PreDrawPixelatedPrimitives(proj, matrices);
            }
        }

        public static void PostDrawProjs(IReadOnlyList<Projectile> projectiles, int startIndex, PrimitiveMatrices matrices)
        {
            for (int i = startIndex; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IDrawPixelatedPrimitivesProjectile m)
                    m.PostDrawPixelatedPrimitives(proj, matrices);

                foreach (IDrawPixelatedPrimitivesProjectile g in PostHook.Enumerate(proj))
                    g.PostDrawPixelatedPrimitives(proj, matrices);
            }
        }
    }
}