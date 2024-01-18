using Microsoft.Xna.Framework;
using SPYoyoMod.Utils;
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

        void DrawPixelatedPrimitives(Projectile proj, Matrix transformMatrix);

        public static void Draw(Matrix transformMatrix)
        {
            foreach (var proj in DrawUtils.GetActiveForDrawProjectiles())
            {
                (proj.ModProjectile as IDrawPixelatedPrimitivesProjectile)?.DrawPixelatedPrimitives(proj, transformMatrix);

                foreach (IDrawPixelatedPrimitivesProjectile g in Hook.Enumerate(proj))
                {
                    g.DrawPixelatedPrimitives(proj, transformMatrix);
                }
            }
        }
    }
}