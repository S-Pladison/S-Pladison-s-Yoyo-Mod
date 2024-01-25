using System;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod
{
    public class ModEvents : ILoadable
    {
        public static event Action OnPostUpdateEverything;
        public static event Action OnWorldUnload;
        public static event Action OnPostDrawDust;

        void ILoadable.Load(Mod mod)
        {
            OnPostUpdateEverything += () => { };
            OnWorldUnload += () => { };
            OnPostDrawDust += () => { };
        }

        void ILoadable.Unload()
        {
            OnPostUpdateEverything = null;
            OnWorldUnload = null;
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

            public override void PostUpdateEverything()
            {
                ModEvents.OnPostUpdateEverything();
            }

            public override void OnWorldUnload()
            {
                ModEvents.OnWorldUnload();
            }
        }
    }
}