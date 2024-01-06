using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod.Common
{
    public interface IPostDrawYoyoString
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook;

        static IPostDrawYoyoString()
        {
            Hook = ProjectileLoader.AddModHook(new GlobalHookList<GlobalProjectile>(typeof(IPostDrawYoyoString).GetMethod(nameof(PostDrawYoyoString))));
        }

        void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter);

        private class HookImplementation : ILoadable
        {
            public void Load(Mod mod)
            {
                On_Main.DrawProj_DrawYoyoString += (orig, main, proj, mountedCenter) =>
                {
                    orig(main, proj, mountedCenter);

                    (proj.ModProjectile as IPostDrawYoyoString)?.PostDrawYoyoString(proj, mountedCenter);

                    foreach (IPostDrawYoyoString g in Hook.Enumerate(proj))
                    {
                        g.PostDrawYoyoString(proj, mountedCenter);
                    }
                };
            }

            public void Unload() { }
        }
    }
}