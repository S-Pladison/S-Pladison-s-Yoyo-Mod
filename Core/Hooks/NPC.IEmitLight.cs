using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using IHook = SPYoyoMod.Core.Hooks.IEmitLightNPC;

namespace SPYoyoMod.Core.Hooks
{
    /// <summary>
    /// Предоставляет возможность NPC излучать свет (даже если игра находится на паузе, как это делают факелы).
    /// <br/>Интерфейс относится к следующим классам: <see cref="ModProjectile"/> и <see cref="GlobalProjectile"/>.
    /// </summary>
    public interface IEmitLightNPC
    {
        internal static readonly GlobalHookList<GlobalNPC> _hook =
            NPCLoader.AddModHook(GlobalHookList<GlobalNPC>.Create(i => ((IHook)i).EmitLight));

        /// <summary>
        /// Предоставляет возможность NPC излучать свет (даже если игра находится на паузе, как это делают факелы).
        /// </summary>
        void EmitLight(NPC npc);

        [Autoload(Side = ModSide.Client)]
        private sealed class EmitLightNPCImplementation : ILoadable
        {
            void ILoadable.Load(Mod mod)
            {
                ModEvents.OnPreDraw += () =>
                {
                    // Устанавливаем переменной Main.gamePaused значение false для корректного излучения света, как это делают факелы;
                    // Не уверен, что это не вызывает проблем, но ошибок я не встречал...

                    // Lighting.AddLight(...)
                    // {
                    //     if (!Main.gamePaused && Main.netMode != 2)
                    //     {
                    //         _activeEngine.AddLight(...);
                    //     }
                    // }

                    var origGamePaused = Main.gamePaused;
                    Main.gamePaused = false;

                    foreach (var npc in Main.ActiveNPCs)
                    {
                        (npc.ModNPC as IHook)?.EmitLight(npc);

                        foreach (IHook g in _hook.Enumerate(npc))
                            g.EmitLight(npc);
                    }

                    Main.gamePaused = origGamePaused;
                };
            }

            void ILoadable.Unload() { }
        }
    }
}
