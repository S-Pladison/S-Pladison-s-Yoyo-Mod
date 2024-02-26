using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace SPYoyoMod
{
    /// <summary>
    /// Class of events useful for mod.
    /// </summary>
    public class ModEvents : ILoadable
    {
        /// <summary>
        /// Called after recipes have been added.
        /// You can use this to edit recipes added by other mods.
        /// </summary>
        public static event Action<Recipe[]> OnPostAddRecipes;

        /// <summary>
        /// Allows you to load things in your system after the mod's content has been setup
        /// (arrays have been resized to fit the content, etc).
        /// </summary>
        public static event Action OnPostSetupContent;

        /// <summary>
        /// Called after the Network got updated, this is the last hook that happens in an update.
        /// </summary>
        public static event Action OnPostUpdateEverything;

        /// <summary>
        /// Called whenever a world is loaded, before <see cref="ModSystem.LoadWorldData(Terraria.ModLoader.IO.TagCompound)"/>.
        /// </summary>
        public static event Action OnWorldLoad;

        /// <summary>
        /// Called whenever a world is unloaded.
        /// </summary>
        public static event Action OnWorldUnload;

        /// <summary>
        /// Called when hardmode starts.
        /// </summary>
        public static event Action OnHardmodeStart;

        /// <summary>
        /// Called before start draw game.
        /// </summary>
        public static event Action OnPreDraw;

        /// <summary>
        /// Called after the game has updated the camera position.
        /// Useful for drawing on render targets.
        /// </summary>
        public static event Action OnPostUpdateCameraPosition;

        /// <summary>
        /// Called after <see cref="Main.DrawDust"/>.
        /// </summary>
        public static event Action OnPostDrawDust;

        /// <summary>
        /// Called after drawing tiles. Can be used for drawing a tile overlay akin to wires.
        /// </summary>
        public static event Action OnPostDrawTiles;

        /// <summary>
        /// Called when screen resolution changes.
        /// </summary>
        public static event Action<Vector2> OnResolutionChanged;

        void ILoadable.Load(Mod mod)
        {
            LoadModEvents();
            LoadVanillaEvents();
            LoadModHooks();
        }

        void ILoadable.Unload()
        {
            UnloadModEvents();
            UnloadVanillaEvents();
        }

        private static void LoadModEvents()
        {
            OnPostAddRecipes += EmptyAction;
            OnPostSetupContent += EmptyAction;
            OnPostUpdateEverything += EmptyAction;
            OnWorldLoad += EmptyAction;
            OnWorldUnload += EmptyAction;
            OnHardmodeStart += EmptyAction;
            OnPostUpdateCameraPosition += EmptyAction;
            OnPostDrawDust += EmptyAction;
            OnPostDrawTiles += EmptyAction;
        }

        private static void UnloadModEvents()
        {
            OnPostAddRecipes = null;
            OnPostSetupContent = null;
            OnPostUpdateEverything = null;
            OnWorldLoad = null;
            OnWorldUnload = null;
            OnHardmodeStart = null;
            OnPostUpdateCameraPosition = null;
            OnPostDrawDust = null;
            OnPostDrawTiles = null;
        }

        private static void LoadVanillaEvents()
        {
            OnResolutionChanged += EmptyAction;
            Main.OnResolutionChanged += ModOnResolutionChanged;

            OnPreDraw += EmptyAction;
            Main.OnPreDraw += ModOnPreDraw;
        }

        private static void UnloadVanillaEvents()
        {
            Main.OnResolutionChanged -= ModOnResolutionChanged;
            OnResolutionChanged = null;

            Main.OnPreDraw -= ModOnPreDraw;
            OnPreDraw = null;
        }

        private static void LoadModHooks()
        {
            On_Main.DoDraw_UpdateCameraPosition += (orig) =>
            {
                orig();
                OnPostUpdateCameraPosition();
            };

            On_Main.DrawDust += (orig, main) =>
            {
                orig(main);
                OnPostDrawDust();
            };
        }

        private static void ModOnResolutionChanged(Vector2 screenSize) => ModEvents.OnResolutionChanged(screenSize);
        private static void ModOnPreDraw(GameTime _) => ModEvents.OnPreDraw();

        private class EventSystem : ModSystem
        {
            public override void PostAddRecipes() => ModEvents.OnPostAddRecipes(Main.recipe);
            public override void PostSetupContent() => ModEvents.OnPostSetupContent();
            public override void PostUpdateEverything() => ModEvents.OnPostUpdateEverything();
            public override void OnWorldLoad() => ModEvents.OnWorldLoad();
            public override void OnWorldUnload() => ModEvents.OnWorldUnload();
            public override void ModifyHardmodeTasks(List<GenPass> list) => ModEvents.OnHardmodeStart();
            public override void PostDrawTiles() => ModEvents.OnPostDrawTiles();
        }

        private static void EmptyAction() { }
        private static void EmptyAction<T>(T _) { }
    }
}