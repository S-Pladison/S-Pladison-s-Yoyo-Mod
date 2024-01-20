using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    public interface IDrawPixelatedProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> PreHook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IDrawPixelatedProjectile).GetMethod(nameof(PreDrawPixelated)))
            );

        public static readonly GlobalHookList<GlobalProjectile> PostHook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IDrawPixelatedProjectile).GetMethod(nameof(PostDrawPixelated)))
            );

        void PreDrawPixelated(Projectile proj) { }
        void PostDrawPixelated(Projectile proj) { }

        public static int FirstProjIndex(IReadOnlyList<Projectile> projectiles, bool preDraw)
        {
            var hook = preDraw ? PreHook : PostHook;

            for (int i = 0; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IDrawPixelatedProjectile)
                    return i;

                foreach (var _ in hook.Enumerate(proj))
                    return i;
            }

            return -1;
        }

        public static void PreDrawProjs(IReadOnlyList<Projectile> projectiles, int startIndex)
        {
            for (int i = startIndex; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IDrawPixelatedProjectile m)
                    m.PreDrawPixelated(proj);

                foreach (IDrawPixelatedProjectile g in PreHook.Enumerate(proj))
                    g.PreDrawPixelated(proj);
            }
        }

        public static void PostDrawProjs(IReadOnlyList<Projectile> projectiles, int startIndex)
        {
            for (int i = startIndex; i < projectiles.Count; i++)
            {
                var proj = projectiles[i];

                if (proj.ModProjectile is IDrawPixelatedProjectile m)
                    m.PostDrawPixelated(proj);

                foreach (IDrawPixelatedProjectile g in PostHook.Enumerate(proj))
                    g.PostDrawPixelated(proj);
            }
        }
    }
}