using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;

namespace SPYoyoMod.Common.UIs
{
    [Autoload(Side = ModSide.Client)]
    public class UISystem : ModSystem
    {
        private static readonly List<UserInterface> userInterfaces;
        private static readonly List<ModUIState> uiStates;

        private static float uiScale;

        static UISystem()
        {
            userInterfaces = new();
            uiStates = new();
            uiScale = -1f;
        }

        public override void SetStaticDefaults()
        {
            uiStates.AddRange(ModContent.GetContent<ModUIState>());

            foreach (var uiState in uiStates)
            {
                var userInterface = new UserInterface();
                userInterface.SetState(uiState);
                userInterfaces.Add(userInterface);
            }
        }

        public override void Load()
        {
            ModEvents.OnResolutionChanged += OnResolutionChanged;
        }

        public override void Unload()
        {
            userInterfaces.Clear();
            uiStates.Clear();
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            for (int i = 0; i < userInterfaces.Count; i++)
            {
                var uiState = uiStates[i];
                var userInterface = userInterfaces[i];

                var index = uiState.InsertionIndex(layers);
                if (index < 0) continue;

                layers.Insert(index, new LegacyGameInterfaceLayer(
                    name: $"{Mod.Name}: {uiState.Name}",
                    drawMethod: () =>
                    {
                        if (uiState.Visible)
                        {
                            userInterface.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                        }
                        return true;
                    },
                    scaleType: uiState.ScaleType)
                );
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (uiScale != Main.UIScale)
            {
                uiScale = Main.UIScale;

                foreach (var uiState in uiStates)
                {
                    uiState.OnUIScaleChanged();
                }
            }

            if (Main.mapFullscreen) return;

            foreach (var userInterface in userInterfaces)
            {
                userInterface.Update(gameTime);
            }
        }

        public void OnResolutionChanged(Vector2 screenSize)
        {
            foreach (var uiState in uiStates)
            {
                uiState.OnResolutionChanged((int)screenSize.X, (int)screenSize.Y);
            }
        }
    }
}