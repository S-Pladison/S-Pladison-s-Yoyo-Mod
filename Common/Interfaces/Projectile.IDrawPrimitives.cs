using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    public interface IDrawPrimitivesProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IDrawPrimitivesProjectile).GetMethod(nameof(DrawPrimitives)))
            );

        void DrawPrimitives(Projectile proj, PrimitiveMatrices matrices);

        public static void Draw(PrimitiveMatrices matrices)
        {
            foreach (var proj in DrawUtils.GetActiveForDrawProjectiles())
            {
                (proj.ModProjectile as IDrawPrimitivesProjectile)?.DrawPrimitives(proj, matrices);

                foreach (IDrawPrimitivesProjectile g in Hook.Enumerate(proj))
                {
                    g.DrawPrimitives(proj, matrices);
                }
            }
        }
    }
}