using Microsoft.Xna.Framework;
using SPYoyoMod.Utils;
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

        void DrawPrimitives(Projectile proj, Matrix transformMatrix);

        public static void Draw(Matrix transformMatrix)
        {
            foreach (var proj in DrawUtils.GetActiveForDrawProjectiles())
            {
                (proj.ModProjectile as IDrawPrimitivesProjectile)?.DrawPrimitives(proj, transformMatrix);

                foreach (IDrawPrimitivesProjectile g in Hook.Enumerate(proj))
                {
                    g.DrawPrimitives(proj, transformMatrix);
                }
            }
        }
    }
}