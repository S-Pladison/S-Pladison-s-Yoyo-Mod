using SPYoyoMod.Utils.DataStructures;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    public interface IDrawPrimitivesProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> PreHook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IDrawPrimitivesProjectile).GetMethod(nameof(PreDrawPrimitives)))
            );

        public static readonly GlobalHookList<GlobalProjectile> PostHook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IDrawPrimitivesProjectile).GetMethod(nameof(PostDrawPrimitives)))
            );

        void PreDrawPrimitives(Projectile proj, PrimitiveMatrices matrices) { }
        void PostDrawPrimitives(Projectile proj, PrimitiveMatrices matrices) { }

        public static int FirstProjIndex(IReadOnlyList<Projectile> projectiles, bool preDraw)
        {
            var hook = preDraw ? PreHook : PostHook;

            for (int i = 0; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IDrawPrimitivesProjectile)
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

                if (proj.ModProjectile is IDrawPrimitivesProjectile m)
                    m.PreDrawPrimitives(proj, matrices);

                foreach (IDrawPrimitivesProjectile g in PreHook.Enumerate(proj))
                    g.PreDrawPrimitives(proj, matrices);
            }
        }

        public static void PostDrawProjs(IReadOnlyList<Projectile> projectiles, int startIndex, PrimitiveMatrices matrices)
        {
            for (int i = startIndex; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IDrawPrimitivesProjectile m)
                    m.PostDrawPrimitives(proj, matrices);

                foreach (IDrawPrimitivesProjectile g in PostHook.Enumerate(proj))
                    g.PostDrawPrimitives(proj, matrices);
            }
        }
    }
}