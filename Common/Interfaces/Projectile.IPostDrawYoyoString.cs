using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common.Interfaces
{
    public interface IPostDrawYoyoStringProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook;

        static IPostDrawYoyoStringProjectile()
        {
            Hook = ProjectileLoader.AddModHook(new GlobalHookList<GlobalProjectile>(typeof(IPostDrawYoyoStringProjectile).GetMethod(nameof(PostDrawYoyoString))));
        }

        void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter);

        private class HookImplementation : ILoadable
        {
            public void Load(Mod mod)
            {
                On_Main.DrawProj_DrawYoyoString += (orig, main, proj, mountedCenter) =>
                {
                    orig(main, proj, mountedCenter);

                    (proj.ModProjectile as IPostDrawYoyoStringProjectile)?.PostDrawYoyoString(proj, mountedCenter);

                    foreach (IPostDrawYoyoStringProjectile g in Hook.Enumerate(proj))
                    {
                        g.PostDrawYoyoString(proj, mountedCenter);
                    }
                };
            }

            public void Unload() { }
        }
    }
}