using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using IHook = SPYoyoMod.Core.Hooks.IEmitLightEntity;

namespace SPYoyoMod.Core.Hooks
{
    /// <summary>
    /// Предоставляет сущностям возможность излучать свет (даже если игра находится на паузе, как это делают факелы).
    /// <br/>Интерфейс относится к следующим классам:
    /// <br/>- <see cref="ModPlayer"/>;
    /// <br/>- <see cref="ModNPC"/> и <see cref="GlobalNPC"/>;
    /// <br/>- <see cref="ModProjectile"/> и <see cref="GlobalProjectile"/>.
    /// </summary>
    public interface IEmitLightEntity
    {
        internal static readonly HookList<ModPlayer> _playerHook =
            PlayerLoader.AddModHook(HookList<ModPlayer>.Create(i => ((IHook)i).EmitLight));

        internal static readonly GlobalHookList<GlobalNPC> _npcHook =
            NPCLoader.AddModHook(GlobalHookList<GlobalNPC>.Create(i => ((IHook)i).EmitLight));

        internal static readonly GlobalHookList<GlobalProjectile> _projHook =
            ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(i => ((IHook)i).EmitLight));

        /// <summary>
        /// Предоставляет сущности возможность излучать свет (даже если игра находится на паузе, как это делают факелы).
        /// </summary>
        void EmitLight(Entity entity);

        [Autoload(Side = ModSide.Client)]
        private sealed class EmitLightEntityImplementation : ILoadable
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

                    foreach (var player in Main.ActivePlayers)
                    {
                        foreach (IHook g in _playerHook.Enumerate(player))
                            g.EmitLight(player);
                    }

                    foreach (var npc in Main.ActiveNPCs)
                    {
                        (npc.ModNPC as IHook)?.EmitLight(npc);

                        foreach (IHook g in _npcHook.Enumerate(npc))
                            g.EmitLight(npc);
                    }

                    foreach (var proj in Main.ActiveProjectiles)
                    {
                        (proj.ModProjectile as IHook)?.EmitLight(proj);

                        foreach (IHook g in _projHook.Enumerate(proj))
                            g.EmitLight(proj);
                    }

                    Main.gamePaused = origGamePaused;
                };
            }

            void ILoadable.Unload() { }
        }
    }
}
