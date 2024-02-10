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
        /// Called when Hardmode starts.
        /// </summary>
        public static event Action OnHardmodeStart;

        /// <summary>
        /// Called before start draw game.
        /// </summary>
        public static event Action OnPreDraw;

        /// <summary>
        /// Called after <see cref="Main.DrawDust"/>.
        /// </summary>
        public static event Action OnPostDrawDust;

        /// <summary>
        /// Called after drawing Tiles. Can be used for drawing a tile overlay akin to wires.
        /// </summary>
        public static event Action OnPostDrawTiles;

        /// <summary>
        /// Called when screen resolution changes.
        /// </summary>
        public static event Action<Vector2> OnResolutionChanged;

        void ILoadable.Load(Mod mod)
        {
            OnPostAddRecipes += (_) => { };
            OnPostSetupContent += () => { };
            OnPostUpdateEverything += () => { };
            OnWorldLoad += () => { };
            OnWorldUnload += () => { };
            OnHardmodeStart += () => { };
            OnPostDrawDust += () => { };
            OnPostDrawTiles += () => { };
            OnResolutionChanged += (_) => { };
            OnPreDraw += () => { };

            On_Main.DrawDust += (orig, main) =>
            {
                orig(main);
                OnPostDrawDust();
            };

            Main.OnResolutionChanged += Mod_OnResolutionChanged;
            Main.OnPreDraw += Mod_OnPreDraw;
        }

        void ILoadable.Unload()
        {
            OnPostAddRecipes = null;
            OnPostSetupContent = null;
            OnPostUpdateEverything = null;
            OnWorldLoad = null;
            OnWorldUnload = null;
            OnHardmodeStart = null;
            OnPostDrawDust = null;
            OnPostDrawTiles = null;
            OnResolutionChanged = null;
            OnPreDraw = null;

            Main.OnResolutionChanged -= Mod_OnResolutionChanged;
            Main.OnPreDraw -= Mod_OnPreDraw;
        }

        private static void Mod_OnResolutionChanged(Vector2 screenSize) => ModEvents.OnResolutionChanged(screenSize);
        private static void Mod_OnPreDraw(GameTime _) => ModEvents.OnPreDraw();

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
    }
}