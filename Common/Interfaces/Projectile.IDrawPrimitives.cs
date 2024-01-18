using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    public interface IDrawPrimitivesProjectile : IPreDrawPrimitivesProjectile, IPostDrawPrimitivesProjectile { }

    public interface IPreDrawPrimitivesProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IPreDrawPrimitivesProjectile).GetMethod(nameof(PreDrawPrimitives)))
            );

        void PreDrawPrimitives(Projectile proj, Matrix transformMatrix);

        public static void Draw(Matrix transformMatrix)
        {
            foreach (var proj in Main.projectile)
            {
                if (!proj.active) continue;

                (proj.ModProjectile as IPreDrawPrimitivesProjectile)?.PreDrawPrimitives(proj, transformMatrix);

                foreach (IPreDrawPrimitivesProjectile g in Hook.Enumerate(proj))
                {
                    g.PreDrawPrimitives(proj, transformMatrix);
                }
            }
        }
    }

    public interface IPostDrawPrimitivesProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IPostDrawPrimitivesProjectile).GetMethod(nameof(PostDrawPrimitives)))
            );

        void PostDrawPrimitives(Projectile proj, Matrix transformMatrix);

        public static void Draw(Matrix transformMatrix)
        {
            foreach (var proj in Main.projectile)
            {
                if (!proj.active) continue;

                (proj.ModProjectile as IPostDrawPrimitivesProjectile)?.PostDrawPrimitives(proj, transformMatrix);

                foreach (IPostDrawPrimitivesProjectile g in Hook.Enumerate(proj))
                {
                    g.PostDrawPrimitives(proj, transformMatrix);
                }
            }
        }
    }
}