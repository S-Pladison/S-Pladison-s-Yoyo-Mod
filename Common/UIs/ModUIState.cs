using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.UI;

namespace SPYoyoMod.Common.UIs
{
    [Autoload(Side = ModSide.Client)]
    public abstract class ModUIState : UIState, ILoadable
    {
        public Mod Mod { get; private set; }
        public string Name { get; private set; }

        public bool Visible
        {
            get => innerVisible;
            set
            {
                if (innerVisible != value)
                {
                    innerVisible = value;

                    if (value) Activate();
                    else Deactivate();
                }
            }
        }

        public InterfaceScaleType ScaleType
        {
            get => innerScaleType;
            set => innerScaleType = value;
        }

        private bool innerVisible;
        private InterfaceScaleType innerScaleType;

        public ModUIState()
        {
            innerVisible = false;
            innerScaleType = InterfaceScaleType.UI;
        }

        public abstract int InsertionIndex(List<GameInterfaceLayer> layers);

        public virtual void OnResolutionChanged(int width, int height) { }
        public virtual void OnUIScaleChanged() { }

        void ILoadable.Load(Mod mod)
        {
            Mod = mod;
            Name = GetType().Name;
        }

        void ILoadable.Unload()
        {
            // ...
        }
    }
}