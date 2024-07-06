using Microsoft.Xna.Framework;
using SPYoyoMod.Utils;
using System;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod
{
    public class ModEvents : ILoadable
    {
        // Mod

        /// <summary>
        /// Позволяет загружать вещи после того, как мод подготовил весь свой контент.
        /// </summary>
        public static event Action OnPostSetupContent;

        /// <summary>
        /// Вызывается после обновления позиции камеры. Полезен для отрисовки на целях рендеринга.
        /// </summary>
        public static event Action OnPostUpdateCameraPosition;

        // Vanilla

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
            LoadModEvents();
            LoadVanillaEvents();
        }

        void ILoadable.Unload()
        {
            UnloadVanillaEvents();
            UnloadModEvents();
        }

        private static void LoadModEvents()
        {
            OnPostSetupContent += ModUtils.EmptyAction;
            OnPostUpdateCameraPosition += ModUtils.EmptyAction;

            On_Main.DoDraw_UpdateCameraPosition += (orig) =>
            {
                orig();
                OnPostUpdateCameraPosition();
            };
        }

        private static void UnloadModEvents()
        {
            OnPostUpdateCameraPosition = null;
            OnPostSetupContent = null;
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

        private sealed class EventSystem : ModSystem
        {
            public override void PostSetupContent() => ModEvents.OnPostSetupContent();
        }
    }
}