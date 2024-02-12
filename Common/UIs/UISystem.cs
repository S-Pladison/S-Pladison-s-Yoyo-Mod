using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.UI;

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

        public override void Load()
        {
            foreach (var type in AssemblyManager.GetLoadableTypes(Mod.Code).Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(ModUIState))))
            {
                var uiState = Activator.CreateInstance(type) as ModUIState;
                uiStates.Add(uiState);

                ContentInstance.Register(uiState);

                var userInterface = new UserInterface();
                userInterfaces.Add(userInterface);

                uiState.Mod = Mod;
                uiState.UserInterface = userInterface;
                userInterface.SetState(uiState);
                uiState.IsVisible = false;
            }

            ModEvents.OnResolutionChanged += OnResolutionChanged;
        }

        public override void Unload()
        {
            foreach (var userInterface in userInterfaces)
            {
                userInterface.SetState(null);
            }

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
                        if (userInterface.CurrentState != null && uiState.IsVisible)
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

                for (int i = 0; i < userInterfaces.Count; i++)
                {
                    var uiState = uiStates[i];
                    var userInterface = userInterfaces[i];

                    if (userInterface.CurrentState == null || !uiState.IsVisible) continue;

                    userInterface.Recalculate();
                }
            }

            for (int i = 0; i < userInterfaces.Count; i++)
            {
                var uiState = uiStates[i];
                var userInterface = userInterfaces[i];

                if (userInterface.CurrentState == null || !uiState.IsVisible) continue;

                userInterface.Update(gameTime);
            }
        }

        public void OnResolutionChanged(Vector2 screenSize)
        {
            for (int i = 0; i < userInterfaces.Count; i++)
            {
                var uiState = uiStates[i];
                var userInterface = userInterfaces[i];

                if (userInterface.CurrentState == null || !uiState.IsVisible) continue;

                userInterface.Recalculate();
            }
        }
    }
}