using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace SPYoyoMod
{
    public class ModEvents : ILoadable
    {
        public static event Action<Recipe[]> OnPostSetupRecipes;
        public static event Action OnPostUpdateEverything;
        public static event Action OnWorldLoad;
        public static event Action OnWorldUnload;
        public static event Action OnHardmodeStart;
        public static event Action OnPostDrawDust;

        void ILoadable.Load(Mod mod)
        {
            OnPostSetupRecipes += (_) => { };
            OnPostUpdateEverything += () => { };
            OnWorldLoad += () => { };
            OnWorldUnload += () => { };
            OnHardmodeStart += () => { };
            OnPostDrawDust += () => { };
        }

        void ILoadable.Unload()
        {
            OnPostSetupRecipes = null;
            OnPostUpdateEverything = null;
            OnWorldLoad = null;
            OnWorldUnload = null;
            OnHardmodeStart = null;
            OnPostDrawDust = null;
        }

        private class EventSystem : ModSystem
        {
            public override void Load()
            {
                On_Main.DrawDust += (orig, main) =>
                {
                    orig(main);
                    OnPostDrawDust();
                };
            }

            public override void PostSetupRecipes() => ModEvents.OnPostSetupRecipes(Main.recipe);
            public override void PostUpdateEverything() => ModEvents.OnPostUpdateEverything();
            public override void OnWorldLoad() => ModEvents.OnWorldLoad();
            public override void OnWorldUnload() => ModEvents.OnWorldUnload();
            public override void ModifyHardmodeTasks(List<GenPass> list) => ModEvents.OnHardmodeStart();
        }
    }
}