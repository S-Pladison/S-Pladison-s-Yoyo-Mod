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
    [LoadPriority(sbyte.MaxValue)]
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

        [LoadPriority(sbyte.MaxValue)]
        private sealed class EventSystem : ModSystem
        {
            private Point _savedScreenSize;
            private bool _savedGameInactive;

            public override void Load()
            {
                // - Почему не используется только ванильный Main.OnResolutionChanged?
                // При загрузке мода с разрешением происходят какиет проблемы,
                // а вызова OnResolutionChanged не происходит.
                // Данный способ хоть и добавляет дополнительную постоянную проверку,
                // но гарантирует, что размер экрана действительно был изменен.
                ModEvents.OnPreDraw += () => ResolutionChangedHandler(Main.ScreenSize.ToVector2());

                Main.OnResolutionChanged += ResolutionChangedHandler;
            }

            public override void Unload()
                => Main.OnResolutionChanged -= ResolutionChangedHandler;

            public override void PostAddRecipes()
                => ModEvents.OnPostSetupRecipes(Main.recipe);

            public override void PostSetupContent()
                => ModEvents.OnPostSetupContent();

            public override void OnWorldUnload()
                => ModEvents.OnWorldUnload();

            public override void PreUpdateDusts()
                => ModEvents.OnPreUpdateDusts();

            public override void PostUpdateEverything()
                => ModEvents.OnPostUpdateEverything();

            private void ResolutionChangedHandler(Vector2 screenSize)
            {
                if (_savedScreenSize != Main.ScreenSize || _savedGameInactive != Main.gameInactive)
                {
                    _savedScreenSize = Main.ScreenSize;
                    _savedGameInactive = Main.gameInactive;

                    ModEvents.OnResolutionChanged(Main.ScreenSize);
                }
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

        [LoadPriority(sbyte.MaxValue)]
        private sealed class EventPlayer : ModPlayer
        {
            public override void PlayerConnect()
            {
                var connectedPlayerIndex = (byte)Main.myPlayer;

                ModEvents.OnPlayerConnect(Main.player[connectedPlayerIndex]);

                NetHandler.Send<PlayerConnectPacket>(null, null, connectedPlayerIndex);
            }

            public override void OnEnterWorld()
            {
                // Костыль, исправляющий проблему с ошибочным разрешением экранных целей рендеринга при входе игрока в мир
                ModEvents.OnResolutionChanged(Main.ScreenSize);
            }
        }
    }
}