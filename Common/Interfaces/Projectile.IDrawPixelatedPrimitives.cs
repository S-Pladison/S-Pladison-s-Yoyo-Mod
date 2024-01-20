using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    public interface IDrawPixelatedPrimitivesProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IDrawPixelatedPrimitivesProjectile).GetMethod(nameof(DrawPixelatedPrimitives)))
            );

        void DrawPixelatedPrimitives(Projectile proj, PrimitiveMatrices matrices);

        public static void Draw(PrimitiveMatrices matrices)
        {
            foreach (var proj in DrawUtils.GetActiveForDrawProjectiles())
            {
                (proj.ModProjectile as IDrawPixelatedPrimitivesProjectile)?.DrawPixelatedPrimitives(proj, matrices);

                foreach (IDrawPixelatedPrimitivesProjectile g in Hook.Enumerate(proj))
                {
                    g.DrawPixelatedPrimitives(proj, matrices);
                }
            }
        }
    }
}