using Microsoft.Xna.Framework;
using SPYoyoMod.Utils;
using System;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod
{
    public class ModEvents : ILoadable
    {
        /// <summary>
        /// Вызывается перед началом отрисовки игры.
        /// </summary>
        public static event Action OnPreDraw;

        /// <summary>
        /// Вызывается при изменении разрешения экрана.
        /// </summary>
        public static event Action<Point> OnResolutionChanged;

        void ILoadable.Load(Mod mod)
        {
            LoadVanillaEvents();
        }

        void ILoadable.Unload()
        {
            UnloadVanillaEvents();
        }

        private static void LoadVanillaEvents()
        {
            OnPreDraw += ModUtils.EmptyAction;
            Main.OnPreDraw += ModOnPreDraw;

            OnResolutionChanged += ModUtils.EmptyAction;
            Main.OnResolutionChanged += ModOnResolutionChanged;
        }

        private static void UnloadVanillaEvents()
        {
            Main.OnResolutionChanged -= ModOnResolutionChanged;
            OnResolutionChanged = null;

            Main.OnPreDraw -= ModOnPreDraw;
            OnPreDraw = null;
        }

        private static void ModOnPreDraw(GameTime _)
            => ModEvents.OnPreDraw();

        private static void ModOnResolutionChanged(Vector2 screenSize)
            => ModEvents.OnResolutionChanged(screenSize.ToPoint());
    }
}