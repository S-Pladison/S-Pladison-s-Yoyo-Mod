using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using IHook = SPYoyoMod.Common.Hooks.IInitializableProjectile;

namespace SPYoyoMod.Common.Hooks
{
    /// <summary>
    /// Позволяет проинициализировать данные/объекты для снаряда.
    /// Вызывается один раз за жизнь снаряда, но при этом, в отличии от <see cref="Projectile.OnSpawn"/>,
    /// гарантированно вызывается для всех игроков.
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
        /// </summary>
        void Initialize(Projectile proj);
    }

    [LoadPriority(sbyte.MinValue)]
    internal sealed class InitializableProjectileImplementation : GlobalProjectile
    {
        private bool _initialized;

        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
        {
            // Ну, я не придумал способа ограничить данный Global лишь для снарядов с интерфейсом, что выше...
            // Да, для ModProjectile это не проблеме, но как это сделать с остальными GlobalProjectile?..
            return true;
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