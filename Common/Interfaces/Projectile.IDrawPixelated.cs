using SPYoyoMod.Utils;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    public interface IDrawPixelatedProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IDrawPixelatedProjectile).GetMethod(nameof(DrawPixelated)))
            );

        void DrawPixelated(Projectile proj);

        public static void Draw()
        {
            foreach (var proj in DrawUtils.GetActiveForDrawProjectiles())
            {
                (proj.ModProjectile as IDrawPixelatedProjectile)?.DrawPixelated(proj);

                foreach (IDrawPixelatedProjectile g in Hook.Enumerate(proj))
                {
                    g.DrawPixelated(proj);
                }
            }
        }
    }
}