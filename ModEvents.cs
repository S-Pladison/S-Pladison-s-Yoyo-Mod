using Microsoft.Xna.Framework;
using SPYoyoMod.Common;
using SPYoyoMod.Utils;
using System;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod
{
    [LoadPriority(sbyte.MaxValue)]
    public sealed class ModEvents : ILoadable
    {
        // Mod

        /// <summary>
        /// Позволяет обрабатывать рецепты после их настройки. Не следует редактировать какой-либо рецепт.
        /// </summary>
        public static event Action<Recipe[]> OnPostSetupRecipes;

        /// <summary>
        /// Позволяет загружать вещи после того, как мод подготовил весь свой контент.
        /// </summary>
        public static event Action OnPostSetupContent;

        /// <summary>
        /// Вызывается перед тем, как пыль будет обновлена.
        /// </summary>
        public static event Action OnPreUpdateDusts;

        /// <summary>
        /// Вызывается после обновления позиции камеры. Полезен для отрисовки на целях рендеринга.
        /// </summary>
        public static event Action OnPostUpdateCameraPosition;

        /// <summary>
        /// Вызывается при изменении разрешения экрана.
        /// </summary>
        public static event Action<Point> OnResolutionChanged;

        // Vanilla

        /// <summary>
        /// Вызывается перед началом отрисовки игры.
        /// </summary>
        public static event Action OnPreDraw;

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
            OnPostSetupRecipes += ModUtils.EmptyAction;
            OnPostSetupContent += ModUtils.EmptyAction;
            OnPreUpdateDusts += ModUtils.EmptyAction;
            OnPostUpdateCameraPosition += ModUtils.EmptyAction;
            OnResolutionChanged += ModUtils.EmptyAction;

            On_Main.DoDraw_UpdateCameraPosition += (orig) =>
            {
                orig();
                OnPostUpdateCameraPosition();
            };
        }

        private static void UnloadModEvents()
        {
            OnResolutionChanged = null;
            OnPostUpdateCameraPosition = null;
            OnPreUpdateDusts = null;
            OnPostSetupContent = null;
            OnPostSetupRecipes = null;
        }

        private static void LoadVanillaEvents()
        {
            OnPreDraw += ModUtils.EmptyAction;
            Main.OnPreDraw += ModOnPreDraw;
        }

        private static void UnloadVanillaEvents()
        {
            Main.OnPreDraw -= ModOnPreDraw;
            OnPreDraw = null;
        }

        private static void ModOnPreDraw(GameTime _)
            => ModEvents.OnPreDraw();

        [LoadPriority(sbyte.MaxValue)]
        private sealed class EventSystem : ModSystem
        {
            private Point _savedScreenSize;

            public override void Load()
            {
                // - Почему не используется только ванильный Main.OnResolutionChanged?
                // При загрузке мода с разрешением происходят какиет проблемы,
                // а вызова OnResolutionChanged не происходит.
                // Данный способ хоть и добавляет дополнительную постоянную проверку,
                // но гарантирует, что размер экрана действительно был изменен.
                ModEvents.OnPreDraw += () => ResolutionChangedHandler(Main.ScreenSize.ToVector2());

                Main.OnResolutionChanged += ResolutionChangedHandler;
            }

            public override void Unload()
                => Main.OnResolutionChanged -= ResolutionChangedHandler;

            public override void PostAddRecipes()
                => ModEvents.OnPostSetupRecipes(Main.recipe);

            public override void OnWorldLoad()
                // Костыль, но без него никак :p
                => ModEvents.OnResolutionChanged(Main.ScreenSize);

            public override void PostSetupContent()
                => ModEvents.OnPostSetupContent();

            public override void PreUpdateDusts()
                => ModEvents.OnPreUpdateDusts();

            private void ResolutionChangedHandler(Vector2 screenSize)
            {
                if (_savedScreenSize != Main.ScreenSize)
                {
                    _savedScreenSize = Main.ScreenSize;
                    ModEvents.OnResolutionChanged(Main.ScreenSize);
                }
            }
        }
    }
}