using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using IHook = SPYoyoMod.Common.Hooks.IModifyYoyoStatsProjectile;

namespace SPYoyoMod.Common.Hooks
{
    public struct YoyoStatModifiers()
    {
        public StatModifier LifeTime = StatModifier.Default;
        public StatModifier MaxRange = StatModifier.Default;

        // Стоит ли сделать и его...
        // public StatModifier TopSpeed;
    }

    /// <summary>
    /// Позволяет модифицировать характеристики всех снарядов йо-йо (время жизни, максимальное расстояние и т.д.).<br/>
    /// Модификаторы параметров йо-йо определяются лишь раз в момент инициализации снаряда.<br/>
    /// Интерфейс относится к следующим классам: <see cref="ModProjectile"/> и <see cref="GlobalProjectile"/>
    /// </summary>
    public interface IModifyYoyoStatsProjectile
    {
        internal static readonly GlobalHookList<GlobalProjectile> _hook =
            ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(i => ((IHook)i).ModifyYoyoStats));

        /// <summary>
        /// Позволяет модифицировать характеристики всех снарядов йо-йо (время жизни, максимальное расстояние и т.д.).<br/>
        /// Модификаторы параметров йо-йо определяются лишь раз в момент инициализации снаряда.<br/>
        /// </summary>
        void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers);

        private sealed class ModifyYoyoStatsImplementation : GlobalProjectile, IInitializableProjectile
        {
            private YoyoStatModifiers _statModifiers;

            public override bool InstancePerEntity => true;

            public override void Load()
            {
                IL_Projectile.AI_099_2 += (il) =>
                {
                    var c = new ILCursor(il);

                    // float num2 = ProjectileID.Sets.YoyosLifeTimeMultiplier[this.type];

                    // IL_00EE: ldsfld    float32[] Terraria.ID.ProjectileID/Sets::YoyosLifeTimeMultiplier
                    // IL_00F3: ldarg.0
                    // IL_00F4: ldfld int32 Terraria.Projectile::'type'
                    // IL_00F9: ldelem.r4
                    // IL_00FA: stloc.s num2

                    var num2Index = -1;

                    if (!c.TryGotoNext(MoveType.After,
                        i => i.MatchLdsfld(typeof(ProjectileID.Sets).GetField("YoyosLifeTimeMultiplier")),
                        i => i.MatchLdarg(0),
                        i => i.MatchLdfld<Projectile>("type"),
                        i => i.MatchLdelemR4(),
                        i => i.MatchStloc(out num2Index)))
                    {

                        ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(ModifyYoyoStatsImplementation)}..{nameof(IL_Projectile.AI_099_2)}\" failed...");
                        return;
                    }

                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldloca, num2Index);
                    c.EmitDelegate(ModifyYoyoLifeTimeValue);

                    // float num7 = ProjectileID.Sets.YoyosMaximumRange[this.type];

                    // IL_0522: ldsfld float32[] Terraria.ID.ProjectileID / Sets::YoyosMaximumRange
                    // IL_0527: ldarg.0
                    // IL_0528: ldfld int32 Terraria.Projectile::'type'
                    // IL_052D: ldelem.r4
                    // IL_052E: stloc.s num10

                    var num10Index = -1;

                    if (!c.TryGotoNext(MoveType.After,
                        i => i.MatchLdsfld(typeof(ProjectileID.Sets).GetField("YoyosMaximumRange")),
                        i => i.MatchLdarg(0),
                        i => i.MatchLdfld<Projectile>("type"),
                        i => i.MatchLdelemR4(),
                        i => i.MatchStloc(out num10Index)))
                    {

                        ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(ModifyYoyoStatsImplementation)}..{nameof(IL_Projectile.AI_099_2)}\" failed...");
                        return;
                    }

                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldloca, num10Index);
                    c.EmitDelegate(ModifyYoyoMaxRangeValue);
                };
            }

            private static void GetYoyoStats(Projectile proj, out YoyoStatModifiers statModifiers)
            {
                statModifiers = new();

                (proj.ModProjectile as IHook)?.ModifyYoyoStats(proj, ref statModifiers);

                foreach (IHook g in _hook.Enumerate(proj))
                    g.ModifyYoyoStats(proj, ref statModifiers);
            }

            private static void ModifyYoyoLifeTimeValue(Projectile proj, ref float lifeTime)
            {
                if (!proj.TryGetGlobalProjectile(out ModifyYoyoStatsImplementation globalProj))
                    return;

                // Не трогаем *бесконечные* йо-йо
                if (lifeTime <= 0)
                    return;

                lifeTime = globalProj._statModifiers.LifeTime.ApplyTo(lifeTime);

                if (lifeTime > 0)
                    return;

                lifeTime = -1;
            }

            private static void ModifyYoyoMaxRangeValue(Projectile proj, ref float maxRange)
            {
                if (!proj.TryGetGlobalProjectile(out ModifyYoyoStatsImplementation globalProj))
                    return;

                maxRange = globalProj._statModifiers.MaxRange.ApplyTo(maxRange);
            }

            public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
                => lateInstantiation && proj.IsYoyo();

            public void Initialize(Projectile proj)
                => GetYoyoStats(proj, out _statModifiers);
        }
    }
}