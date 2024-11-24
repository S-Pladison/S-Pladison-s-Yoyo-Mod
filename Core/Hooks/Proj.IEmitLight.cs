using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using IHook = SPYoyoMod.Core.Hooks.IEmitLightProjectile;

namespace SPYoyoMod.Core.Hooks
{
    /// <summary>
    /// Предоставляет возможность снаряду излучать свет (даже если игра находится на паузе, как это делают факелы).
    /// <br/>Интерфейс относится к следующим классам: <see cref="ModProjectile"/> и <see cref="GlobalProjectile"/>.
    /// </summary>
    public interface IEmitLightProjectile
    {
        internal static readonly GlobalHookList<GlobalProjectile> _hook =
            ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(i => ((IHook)i).EmitLight));

        /// <summary>
        /// Предоставляет возможность снаряду излучать свет (даже если игра находится на паузе, как это делают факелы).
        /// </summary>
        void EmitLight(Projectile proj);

        [Autoload(Side = ModSide.Client)]
        private sealed class EmitLightProjectileImplementation : ILoadable
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

                    foreach (var proj in Main.ActiveProjectiles)
                    {
                        (proj.ModProjectile as IHook)?.EmitLight(proj);

                        foreach (IHook g in _hook.Enumerate(proj))
                            g.EmitLight(proj);
                    }

                    Main.gamePaused = origGamePaused;
                };
            }

            void ILoadable.Unload() { }
        }
    }
}
