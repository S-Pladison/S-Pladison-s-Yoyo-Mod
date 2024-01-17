using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    public interface IDrawPixelatedProjectile : IPreDrawPixelatedProjectile, IPostDrawPixelatedProjectile { }

    public interface IPreDrawPixelatedProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook;

        static IPreDrawPixelatedProjectile()
        {
            Hook = ProjectileLoader.AddModHook(new GlobalHookList<GlobalProjectile>(typeof(IPreDrawPixelatedProjectile).GetMethod(nameof(PreDrawPixelated))));
        }

        void PreDrawPixelated(Projectile proj);

        public static void Invoke()
        {
            foreach (var proj in Main.projectile)
            {
                if (!proj.active) continue;

                (proj.ModProjectile as IPreDrawPixelatedProjectile)?.PreDrawPixelated(proj);

                foreach (IPreDrawPixelatedProjectile g in Hook.Enumerate(proj))
                {
                    g.PreDrawPixelated(proj);
                }
            }
        }
    }

    public interface IPostDrawPixelatedProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook;

        static IPostDrawPixelatedProjectile()
        {
            Hook = ProjectileLoader.AddModHook(new GlobalHookList<GlobalProjectile>(typeof(IPostDrawPixelatedProjectile).GetMethod(nameof(PostDrawPixelated))));
        }

        void PostDrawPixelated(Projectile proj);

        public static void Invoke()
        {
            foreach (var proj in Main.projectile)
            {
                if (!proj.active) continue;

                (proj.ModProjectile as IPostDrawPixelatedProjectile)?.PostDrawPixelated(proj);

                foreach (IPostDrawPixelatedProjectile g in Hook.Enumerate(proj))
                {
                    g.PostDrawPixelated(proj);
                }
            }
        }
    }
}