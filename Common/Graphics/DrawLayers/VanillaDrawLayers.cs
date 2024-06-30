using MonoMod.Cil;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
using Mono.Cecil.Cil;

namespace SPYoyoMod.Common.Graphics.DrawLayers
{
    /// <summary>
    /// Класс предоставляет список всех доступных ванильных слоев отрисовки игры.
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public sealed class VanillaDrawLayers : ILoadable
    {
        /// <summary>
        /// Расположение в коде:
        /// <br/>- DrawProjectiles()
        /// <br/>- DrawPlayers_AfterProjectiles()
        /// <br/>=| DrawCachedProjs(DrawCacheProjsOverPlayers)
        /// <br/>- ParticleSystem_World_OverPlayers.Draw(spriteBatch)
        /// <br/>- DrawCachedNPCs(DrawCacheNPCsOverPlayers, behindTiles: false)
        /// </summary>
        public static GameDrawLayer DrawCachedProjs_OverPlayers { get; private set; }

        void ILoadable.Load(Mod mod)
        {
            mod.AddContent(DrawCachedProjs_OverPlayers = new VanillaDrawLayer(nameof(DrawCachedProjs_OverPlayers)));

            LoadHooks();
        }

        void ILoadable.Unload()
        {
            DrawCachedProjs_OverPlayers = null;
        }

        private static void LoadHooks()
        {
            // - Зачем они нужны, если можно сделать то же самое, но с On_Main?
            // Не хочу делать постоянные проверки/поиска нужного списка...
            // Такая проблема есть у таких методов, как DrawCachedNPCs и DrawCachedProjs (а может и еще есть, хз)

            IL_Main.DoDraw += (il) =>
            {
                var cursor = new ILCursor(il);

                Impl_DrawCachedProjs_OverPlayers(cursor);
            };

            IL_Main.DrawCapture += (il) =>
            {
                var cursor = new ILCursor(il);

                Impl_DrawCachedProjs_OverPlayers(cursor);
            };
        }

        private static void Impl_DrawCachedProjs_OverPlayers(ILCursor cursor)
        {
            // DrawCachedProjs(DrawCacheProjsOverPlayers);

            // IL_1762: ldarg.0
            // IL_1763: ldarg.0
            // IL_1764: ldfld class [System.Collections] System.Collections.Generic.List`1<int32> Terraria.Main::DrawCacheProjsOverPlayers
            // IL_1769: ldc.i4.1
            // IL_176a: call instance void Terraria.Main::DrawCachedProjs(class [System.Collections] System.Collections.Generic.List`1<int32>, bool)

            if (!cursor.TryGotoNext(
                MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Main>("DrawCacheProjsOverPlayers"),
                i => i.MatchLdcI4(1),
                i => i.MatchCall(typeof(Main).GetMethod("DrawCachedProjs", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(List<int>), typeof(bool)])))) {

                ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(Impl_DrawCachedProjs_OverPlayers)}\" failed...");
                return;
            }

            var beforeIndex = cursor.Index - 5;

            cursor.Emit(OpCodes.Call, typeof(VanillaDrawLayers).GetProperty(nameof(DrawCachedProjs_OverPlayers), BindingFlags.Static | BindingFlags.Public).GetGetMethod());
            cursor.Emit(OpCodes.Call, typeof(VanillaDrawLayers).GetMethod(nameof(DrawChildAfter), BindingFlags.Static | BindingFlags.NonPublic));

            cursor.Index = beforeIndex;

            cursor.Emit(OpCodes.Call, typeof(VanillaDrawLayers).GetProperty(nameof(DrawCachedProjs_OverPlayers), BindingFlags.Static | BindingFlags.Public).GetGetMethod());
            cursor.Emit(OpCodes.Call, typeof(VanillaDrawLayers).GetMethod(nameof(DrawChildBefore), BindingFlags.Static | BindingFlags.NonPublic));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DrawChildBefore(GameDrawLayer layer)
        {
            foreach (var child in layer.ChildrenBefore)
                child.DrawWithChildren();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DrawChildAfter(GameDrawLayer layer)
        {
            foreach (var child in layer.ChildrenAfter)
                child.DrawWithChildren();
        }

        /// <summary>
        /// Класс ванильного слоя. Видимость всегда на true, т.к. отменять ванильную отрисовку мне не нужно.
        /// </summary>
        [Autoload(false)]
        private sealed class VanillaDrawLayer : GameDrawLayer
        {
            private readonly string _layerName;

            public override string Name => $"{base.Name}_{_layerName}";

            public VanillaDrawLayer(string name)
            {
                _layerName = name;
            }

            public override Position GetDefaultPosition()
                => null;

            public override bool GetDefaultVisibility()
                => true;

            protected override void Draw() { }
        }
    }
}