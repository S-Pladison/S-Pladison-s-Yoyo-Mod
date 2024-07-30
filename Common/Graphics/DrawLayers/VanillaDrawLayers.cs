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
        /// <br/>- DrawSuperSpecialProjectiles(DrawCacheFirstFractals)
        /// <br/>- DrawCachedProjs(DrawCacheProjsBehindProjectiles)
        /// <br/>=| DrawProjectiles()
        /// <br/>- ParticleSystem_World_BehindPlayers.Draw(spriteBatch)
        /// <br/>- DrawPlayers_AfterProjectiles()
        /// </summary>
        public static GameDrawLayer DrawProjectiles { get; private set; }

        /// <summary>
        /// Расположение в коде:
        /// <br/>- DrawProjectiles()
        /// <br/>- ParticleSystem_World_BehindPlayers.Draw(spriteBatch)
        /// <br/>=| DrawPlayers_AfterProjectiles()
        /// <br/>- DrawCachedProjs(DrawCacheProjsOverPlayers)
        /// <br/>- ParticleSystem_World_OverPlayers.Draw(spriteBatch)
        /// </summary>
        public static GameDrawLayer DrawPlayers_AfterProjectiles { get; private set; }

        /// <summary>
        /// Расположение в коде:
        /// <br/>- ParticleSystem_World_BehindPlayers.Draw(spriteBatch)
        /// <br/>- DrawPlayers_AfterProjectiles()
        /// <br/>=| DrawCachedProjs(DrawCacheProjsOverPlayers)
        /// <br/>- ParticleSystem_World_OverPlayers.Draw(spriteBatch)
        /// <br/>- DrawCachedNPCs(DrawCacheNPCsOverPlayers, behindTiles: false)
        /// </summary>
        public static GameDrawLayer DrawCachedProjs_OverPlayers { get; private set; }

        /// <summary>
        /// Расположение в коде:
        /// <br/>- DrawRain()
        /// <br/>- DrawGore()
        /// <br/>=| DrawDust()
        /// <br/>- Overlays.Scene.Draw(spriteBatch, RenderLayers.Entities)
        /// <br/>- DrawWaters() или spriteBatch.Draw(waterTarget, sceneWaterPos - screenPosition, Color.White)
        /// </summary>
        public static GameDrawLayer DrawDust { get; private set; }

        void ILoadable.Load(Mod mod)
        {
            mod.AddContent(DrawProjectiles = new VanillaDrawLayer(nameof(DrawProjectiles)));
            mod.AddContent(DrawPlayers_AfterProjectiles = new VanillaDrawLayer(nameof(DrawPlayers_AfterProjectiles)));
            mod.AddContent(DrawCachedProjs_OverPlayers = new VanillaDrawLayer(nameof(DrawCachedProjs_OverPlayers)));
            mod.AddContent(DrawDust = new VanillaDrawLayer(nameof(DrawDust)));

            LoadHooks();
        }

        void ILoadable.Unload()
        {
            DrawDust = null;
            DrawCachedProjs_OverPlayers = null;
            DrawPlayers_AfterProjectiles = null;
            DrawProjectiles = null;
        }

        private static void LoadHooks()
        {
            On_Main.DrawProjectiles += (orig, main) =>
            {
                DrawChildBefore(DrawProjectiles);
                orig(main);
                DrawChildAfter(DrawProjectiles);
            };

            On_Main.DrawPlayers_AfterProjectiles += (orig, main) =>
            {
                DrawChildBefore(DrawPlayers_AfterProjectiles);
                orig(main);
                DrawChildAfter(DrawPlayers_AfterProjectiles);
            };

            On_Main.DrawDust += (orig, main) =>
            {
                DrawChildBefore(DrawDust);
                orig(main);
                DrawChildAfter(DrawDust);
            };

            // - Зачем нужны IL_Main, если можно сделать то же самое, но с On_Main?
            // Не хочу делать постоянные проверки/поиски нужного списка...
            // Пример:
            // void DrawCachedProjs(List<int> projs) {
            //   if (projs == Main.instance.DrawCacheProjsBehindNPCs) {}
            //   else (projs == Main.instance.DrawCacheProjsBehindNPCsAndTiles) {}
            //   else (...) {}
            //   ...
            // }
            // А текущим методом: просто вызовем наши функции без каких либо проверок
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