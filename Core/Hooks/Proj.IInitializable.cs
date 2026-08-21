using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using IHook = SPYoyoMod.Core.Hooks.IInitializableProjectile;

namespace SPYoyoMod.Core.Hooks
{
    /// <summary>
    /// Позволяет проинициализировать данные/объекты для снаряда.
    /// Вызывается один раз за жизнь снаряда, но при этом, в отличии от <see cref="Projectile.OnSpawn"/>,
    /// гарантированно вызывается для всех игроков.
    /// Примечание: вызывается не в момент появления снаряда, а в момент первого вызова AI.
    /// <br/>Интерфейс относится к следующим классам: <see cref="ModProjectile"/> и <see cref="GlobalProjectile"/>
    /// </summary>
    public interface IInitializableProjectile
    {
        internal static readonly GlobalHookList<GlobalProjectile> _hook =
            ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(i => ((IHook)i).Initialize));

        /// <summary>
        /// Позволяет проинициализировать данные/объекты для снаряда.
        /// Вызывается один раз за жизнь снаряда, но при этом, в отличии от <see cref="Projectile.OnSpawn"/>,
        /// гарантированно вызывается для всех игроков.
        /// Примечание: вызывается не в момент появления снаряда, а в момент первого вызова AI.
        /// </summary>
        void Initialize(Projectile proj);

        [LoadPriority(sbyte.MinValue)]
        private sealed class InitializableProjectileImplementation : GlobalProjectile
        {
            private static GlobalProjectile[] _initializableGlobals;

            public static IReadOnlyList<GlobalProjectile> InitializableGlobals
            {
                get
                {
                    if (_initializableGlobals is not null)
                        return _initializableGlobals;

                    var globals = new List<GlobalProjectile>();

                    foreach (var global in ModContent.GetContent<GlobalProjectile>())
                    {
                        if (global is IHook)
                            globals.Add(global);
                    }

                    return _initializableGlobals = [.. globals];
                }
            }

            private bool _initialized;

            public override bool InstancePerEntity => true;

            public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
            {
                if (!lateInstantiation)
                    return false;

                if (proj.ModProjectile is IHook)
                    return true;

                foreach (var global in InitializableGlobals)
                {
                    if (!global.ConditionallyAppliesToEntities)
                        return true;

                    if (global.AppliesToEntity(proj, lateInstantiation: false) || global.AppliesToEntity(proj, lateInstantiation: true))
                        return true;
                }

                return false;
            }

            public override void Unload()
            {
                _initializableGlobals = null;
            }

            public override void Load()
            {
                MonoModHooks.Add(typeof(ProjectileLoader).GetMethod(nameof(ProjectileLoader.ProjectileAI), BindingFlags.Public | BindingFlags.Static), static (orig_ProjectileLoader_AI orig, Projectile proj) =>
                {
                    if (proj.TryGetGlobalProjectile(out InitializableProjectileImplementation globalProj) && !globalProj._initialized)
                    {
                        (proj.ModProjectile as IHook)?.Initialize(proj);

                        foreach (IHook g in IHook._hook.Enumerate(proj))
                        {
                            g.Initialize(proj);
                        }

                        globalProj._initialized = true;
                    }

                    orig(proj);
                });
            }

            private delegate void orig_ProjectileLoader_AI(Projectile proj);
        }
    }
}