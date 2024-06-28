using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using IHook = SPYoyoMod.Common.Hooks.IPostDrawYoyoStringProjectile;

namespace SPYoyoMod.Common.Hooks
{
    /// <summary>
    /// Позволяет рисовать поверх ниток всех снарядов йо-йо (включая противовесы).<br/>
    /// Интерфейс относится к следующим классам: <see cref="ModProjectile"/> и <see cref="GlobalProjectile"/>
    /// </summary>
    public interface IPostDrawYoyoStringProjectile
    {
        internal static readonly GlobalHookList<GlobalProjectile> _hook =
            ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(i => ((IHook)i).PostDrawYoyoString));

        /// <summary>
        /// Позволяет рисовать поверх ниток всех снарядов йо-йо (включая противовесы).<br/>
        /// Естественно, если снаряд не является йо-йо (или противовесом), то вызываться данная функция не будет.
        /// </summary>
        void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter);
    }

    [Autoload(Side = ModSide.Client)]
    internal sealed class PostDrawYoyoStringImplementation : ILoadable
    {
        public static void DrawYoyoString(On_Main.orig_DrawProj_DrawYoyoString orig, Main main, Projectile proj, Vector2 mountedCenter)
        {
            orig(main, proj, mountedCenter);

            (proj.ModProjectile as IHook)?.PostDrawYoyoString(proj, mountedCenter);

            foreach (IHook g in IHook._hook.Enumerate(proj))
            {
                g.PostDrawYoyoString(proj, mountedCenter);
            }
        }

        public void Load(Mod mod)
        {
            On_Main.DrawProj_DrawYoyoString += DrawYoyoString;
        }

        public void Unload() { }
    }
}