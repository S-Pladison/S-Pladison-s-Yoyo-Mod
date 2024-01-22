using System;
using Terraria.ModLoader;

namespace SPYoyoMod
{
    public class ModEvents : ILoadable
    {
        public static event Action OnPostUpdateEverything;
        public static event Action OnWorldUnload;

        void ILoadable.Load(Mod mod)
        {
            OnPostUpdateEverything += () => { };
            OnWorldUnload += () => { };
        }

        void ILoadable.Unload()
        {
            OnPostUpdateEverything = null;
            OnWorldUnload = null;
        }

        private class EventSystem : ModSystem
        {
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