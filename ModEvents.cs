using Microsoft.Xna.Framework;
using SPYoyoMod.Core;
using SPYoyoMod.Core.Netcode;
using SPYoyoMod.Utils;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod
{
    [LoadBefore]
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
        /// Вызывается всякий раз, как игрок подключается к игре; Вызов происходит для всех, как для клиентов, так и для сервера.
        /// </summary>
        public static event Action<Player> OnPlayerConnect;

        /// <summary>
        /// Вызывается при выгрузке мира.
        /// </summary>
        public static event Action OnWorldUnload;

        /// <summary>
        /// Вызывается перед тем, как пыль будет обновлена.
        /// </summary>
        public static event Action OnPreUpdateDusts;

        /// <summary>
        /// Вызывается после того, как Network был обновлен. Это событие является последним из всех, что вызываются при обновлении игры.
        /// </summary>
        public static event Action OnPostUpdateEverything;

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
            OnPostSetupRecipes += GeneralUtils.EmptyAction;
            OnPostSetupContent += GeneralUtils.EmptyAction;
            OnWorldUnload += GeneralUtils.EmptyAction;
            OnPreUpdateDusts += GeneralUtils.EmptyAction;
            OnPostUpdateEverything += GeneralUtils.EmptyAction;
            OnPostUpdateCameraPosition += GeneralUtils.EmptyAction;
            OnResolutionChanged += GeneralUtils.EmptyAction;

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
            OnPostUpdateEverything = null;
            OnPreUpdateDusts = null;
            OnWorldUnload = null;
            OnPostSetupContent = null;
            OnPostSetupRecipes = null;
        }

        private static void LoadVanillaEvents()
        {
            OnPreDraw += GeneralUtils.EmptyAction;
            Main.OnPreDraw += ModOnPreDraw;
        }

        private static void UnloadVanillaEvents()
        {
            Main.OnPreDraw -= ModOnPreDraw;
            OnPreDraw = null;
        }

        private static void ModOnPreDraw(GameTime _)
            => ModEvents.OnPreDraw();

        [LoadBefore, LoadAfter(typeof(ModEvents))]
        private sealed class EventSystem : ModSystem
        {
            private Point _savedScreenSize;

            public override void Unload()
            {
                if (Main.dedServ)
                    return;

                Main.OnResolutionChanged -= VanillaResolutionChanged;
                ModEvents.OnPreDraw -= CheckResolution;
            }

            public override void PostAddRecipes()
                => ModEvents.OnPostSetupRecipes(Main.recipe);

            public override void PostSetupContent()
            {
                if (!Main.dedServ)
                {
                    ModEvents.OnPreDraw += CheckResolution;
                    Main.OnResolutionChanged += VanillaResolutionChanged;

                    CheckResolution();
                }

                ModEvents.OnPostSetupContent();
            }

            public override void OnWorldUnload()
                => ModEvents.OnWorldUnload();

            public override void PreUpdateDusts()
                => ModEvents.OnPreUpdateDusts();

            public override void PostUpdateEverything()
            {
                ModEvents.OnPostUpdateEverything();
            }

            private void VanillaResolutionChanged(Vector2 _)
            {
                CheckResolution();
            }

            private void CheckResolution()
            {
                var screenSize = GetActualScreenSize();

                if (screenSize.X <= 0 || screenSize.Y <= 0)
                    return;

                if (_savedScreenSize == screenSize)
                    return;

                _savedScreenSize = screenSize;

                ModEvents.OnResolutionChanged(screenSize);

                ModContent.GetInstance<SPYoyoMod>().Logger.Info($":D {screenSize}");
            }

            private static Point GetActualScreenSize()
            {
                var width = Main.screenWidth;
                var height = Main.screenHeight;
                var device = Main.graphics?.GraphicsDevice;

                if (device is not null)
                {
                    var backBufferWidth = device.PresentationParameters.BackBufferWidth;
                    var backBufferHeight = device.PresentationParameters.BackBufferHeight;

                    if (backBufferWidth > 0 && backBufferHeight > 0)
                    {
                        width = backBufferWidth;
                        height = backBufferHeight;
                    }
                }

                return new Point(width, height);
            }
        }

        private sealed class PlayerConnectPacket : NetPacket
        {
            public override void Send(BinaryWriter writer, params object[] context)
            {
                writer.Write((byte)context[0]); //< connectedPlayerIndex
            }

            public override void Receive(BinaryReader reader, int sender)
            {
                var connectedPlayerIndex = reader.ReadByte();

                ModEvents.OnPlayerConnect(Main.player[connectedPlayerIndex]);

                if (Main.netMode == NetmodeID.Server)
                    NetHandler.Send<PlayerConnectPacket>(null, sender, connectedPlayerIndex);
            }
        }

        [LoadBefore, LoadAfter(typeof(ModEvents))]
        private sealed class EventPlayer : ModPlayer
        {
            public override void PlayerConnect()
            {
                var connectedPlayerIndex = (byte)Main.myPlayer;

                ModEvents.OnPlayerConnect(Main.player[connectedPlayerIndex]);

                NetHandler.Send<PlayerConnectPacket>(null, null, connectedPlayerIndex);
            }
        }
    }
}