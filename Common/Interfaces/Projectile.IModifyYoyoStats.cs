using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Common.ModCompatibility;
using SPYoyoMod.Utils;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using static Mono.Cecil.Cil.OpCodes;

namespace SPYoyoMod.Common.Interfaces
{
    /// <summary>
    /// This interface allows you to modify life time or max range for all yoyo projs, including vanilla yoyos.
    /// </summary>
    public interface IModifyYoyoStatsProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook =
            ProjectileLoader.AddModHook(
                new GlobalHookList<GlobalProjectile>(typeof(IModifyYoyoStatsProjectile).GetMethod(nameof(ModifyYoyoStats)))
            );

        /// <summary>
        /// Allows you to modify life time or max range of the yoyo.
        /// Although it is called with every call of Projectile.AI(), it is better not to make dynamic changes, such as 
        /// increasing max range depending on the time of day.
        /// </summary>
        void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers);

        private class ModifyYoyoStatsImplementation : ILoadable
        {
            public void Load(Mod mod)
            {
                IL_Projectile.AI_099_2 += (il) =>
                {
                    var c = new ILCursor(il);

                    // YoyoStatModifiers statModifiers;

                    var statModifiersIndex = il.Body.Variables.Count;

                    il.Body.Variables.Add(new VariableDefinition(c.Context.Import(typeof(YoyoStatModifiers))));

                    c.Emit(Ldarg_0);
                    c.Emit(Ldloca, statModifiersIndex);
                    c.EmitDelegate<GetYoyoStatModifiersDelegate>(GetYoyoStatModifiers);

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
                        i => i.MatchStloc(out num2Index))) return;

                    c.Emit(Ldarg_0);
                    c.Emit(Ldloca, statModifiersIndex);
                    c.Emit(Ldloca, num2Index);
                    c.EmitDelegate<ModifyYoyoStatDelegate>(ModifyYoyoLifeTime);

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
                        i => i.MatchStloc(out num10Index))) return;

                    c.Emit(Ldarg_0);
                    c.Emit(Ldloca, statModifiersIndex);
                    c.Emit(Ldloca, num10Index);
                    c.EmitDelegate<ModifyYoyoStatDelegate>(ModifyYoyoMaxRange);
                };

                ModContent.GetInstance<ThoriumCompatibility>()?.AddHook("Projectiles.ProjectileExtras", "YoyoAI", (orig_ThoriumYoyoAIMethod orig, int projIndex, float lifeTimeSec, float maxRange, float topSpeed, float rotSpeed, Delegate _uAct, Delegate _uAct2) =>
                {
                    var statModifiers = YoyoStatModifiers.Default;
                    var lifeTime = lifeTimeSec * 60;
                    ref var proj = ref Main.projectile[projIndex];

                    GetYoyoStatModifiers(proj, ref statModifiers);
                    ModifyYoyoLifeTime(proj, ref statModifiers, ref lifeTime);
                    ModifyYoyoMaxRange(proj, ref statModifiers, ref maxRange);

                    orig(projIndex, lifeTime / 60, maxRange, topSpeed, rotSpeed, _uAct, _uAct2);
                });
            }

            public void Unload() { }

            public static void GetYoyoStatModifiers(Projectile proj, ref YoyoStatModifiers statModifiers)
            {
                statModifiers = YoyoStatModifiers.Default;

                (proj.ModProjectile as IModifyYoyoStatsProjectile)?.ModifyYoyoStats(proj, ref statModifiers);

                foreach (IModifyYoyoStatsProjectile g in Hook.Enumerate(proj))
                    g.ModifyYoyoStats(proj, ref statModifiers);
            }

            public static void ModifyYoyoLifeTime(Projectile _, ref YoyoStatModifiers statModifiers, ref float lifeTime)
            {
                if (lifeTime <= 0) return;

                lifeTime = statModifiers.LifeTime.ApplyTo(lifeTime);
            }

            public static void ModifyYoyoMaxRange(Projectile _, ref YoyoStatModifiers statModifiers, ref float maxRange)
            {
                maxRange = statModifiers.MaxRange.ApplyTo(maxRange);
            }

            public delegate void GetYoyoStatModifiersDelegate(Projectile proj, ref YoyoStatModifiers statModifiers);
            public delegate void ModifyYoyoStatDelegate(Projectile proj, ref YoyoStatModifiers statModifiers, ref float value);

            public delegate void orig_ThoriumYoyoAIMethod(int projIndex, float lifeTimeSeconds, float maxRange, float topSpeed, float rotateSpeed, Delegate _unknownAction, Delegate _unknownAction2);
        }
    }
}