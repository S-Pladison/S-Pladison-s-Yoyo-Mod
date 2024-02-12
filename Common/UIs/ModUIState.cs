using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.UI;

namespace SPYoyoMod.Common.UIs
{
    [Autoload(Side = ModSide.Client)]
    public abstract class ModUIState : UIState
    {
        public string Name { get; private set; }
        public Mod Mod { get; internal set; }
        public UserInterface UserInterface { get; internal set; }

        public bool IsVisible
        {
            get => visible;
            set
            {
                if (visible != value)
                {
                    visible = value;

                    if (value) OnChangeVisibleStateToTrue();
                    else OnChangeVisibleStateToFalse();
                }
            }
        }

        public InterfaceScaleType ScaleType { get; set; }

        private bool visible;

        public ModUIState()
        {
            Name = GetType().Name;
            ScaleType = InterfaceScaleType.UI;
        }

        public abstract int InsertionIndex(List<GameInterfaceLayer> layers);

        public virtual void OnChangeVisibleStateToTrue() { }
        public virtual void OnChangeVisibleStateToFalse() { }
        public virtual bool PreDraw() { return true; }
        public virtual void PostDraw() { }

        public sealed override void Draw(SpriteBatch spriteBatch)
        {
            if (PreDraw())
            {
                base.Draw(spriteBatch);
            }
            PostDraw();
        }
    }
}