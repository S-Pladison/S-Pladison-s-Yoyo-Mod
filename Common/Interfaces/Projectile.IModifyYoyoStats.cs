using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Common.ModCompatibility;
using SPYoyoMod.Utils.DataStructures;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using static Mono.Cecil.Cil.OpCodes;

namespace SPYoyoMod.Common.Interfaces
{
    public interface IModifyYoyoStatsProjectile
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook;

        static IModifyYoyoStatsProjectile()
        {
            Hook = ProjectileLoader.AddModHook(new GlobalHookList<GlobalProjectile>(typeof(IModifyYoyoStatsProjectile).GetMethod(nameof(ModifyYoyoStats))));
        }

        void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers);

        public static void GetModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            (proj.ModProjectile as IModifyYoyoStatsProjectile)?.ModifyYoyoStats(proj, ref statModifiers);

            foreach (IModifyYoyoStatsProjectile g in Hook.Enumerate(proj))
            {
                g.ModifyYoyoStats(proj, ref statModifiers);
            }
        }

        public static void ModifyYoyoLifeTime(Projectile proj, ref YoyoStatModifiers statModifiers, ref float lifeTime)
        {
            if (lifeTime <= 0) return;

            lifeTime = statModifiers.LifeTime.ApplyTo(lifeTime);
        }

        public static void ModifyYoyoMaxRange(Projectile proj, ref YoyoStatModifiers statModifiers, ref float maxRange)
        {
            maxRange = statModifiers.MaxRange.ApplyTo(maxRange);
        }
    }

    public class ModifyYoyoStatsImplementation : ILoadable
    {
        public void Load(Mod mod)
        {
            IL_Projectile.AI_099_2 += (il) =>
            {
                var c = new ILCursor(il);

                // YoyoStatModifiers statModifiers;

                int statModifiersIndex = il.Body.Variables.Count;

                il.Body.Variables.Add(new VariableDefinition(c.Context.Import(typeof(YoyoStatModifiers))));

                c.Emit(Ldarg_0);
                c.Emit(Ldloca, statModifiersIndex);
                c.EmitDelegate<GetYoyoStatModifierDelegate>(GetYoyoStatModifier);

                // float num2 = ProjectileID.Sets.YoyosLifeTimeMultiplier[this.type];

                // IL_00EE: ldsfld    float32[] Terraria.ID.ProjectileID/Sets::YoyosLifeTimeMultiplier
                // IL_00F3: ldarg.0
                // IL_00F4: ldfld int32 Terraria.Projectile::'type'
                // IL_00F9: ldelem.r4
                // IL_00FA: stloc.s num2

                int num2Index = -1;

                if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdsfld(typeof(ProjectileID.Sets).GetField("YoyosLifeTimeMultiplier")),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Projectile>("type"),
                    i => i.MatchLdelemR4(),
                    i => i.MatchStloc(out num2Index))) return;

                c.Emit(Ldarg_0);
                c.Emit(Ldloca, statModifiersIndex);
                c.Emit(Ldloca, num2Index);
                c.EmitDelegate<ModifyYoyoStatDelegate>(IModifyYoyoStatsProjectile.ModifyYoyoLifeTime);

                // float num7 = ProjectileID.Sets.YoyosMaximumRange[this.type];

                // IL_0522: ldsfld float32[] Terraria.ID.ProjectileID / Sets::YoyosMaximumRange
                // IL_0527: ldarg.0
                // IL_0528: ldfld int32 Terraria.Projectile::'type'
                // IL_052D: ldelem.r4
                // IL_052E: stloc.s num10

                int num10Index = -1;
                if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdsfld(typeof(ProjectileID.Sets).GetField("YoyosMaximumRange")),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Projectile>("type"),
                    i => i.MatchLdelemR4(),
                    i => i.MatchStloc(out num10Index))) return;

                c.Emit(Ldarg_0);
                c.Emit(Ldloca, statModifiersIndex);
                c.Emit(Ldloca, num10Index);
                c.EmitDelegate<ModifyYoyoStatDelegate>(IModifyYoyoStatsProjectile.ModifyYoyoMaxRange);
            };
        }

        public void GetYoyoStatModifier(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            statModifiers = YoyoStatModifiers.Default;

            IModifyYoyoStatsProjectile.GetModifyYoyoStats(proj, ref statModifiers);
        }

        public void Unload() { }

        private delegate void GetYoyoStatModifierDelegate(Projectile proj, ref YoyoStatModifiers statModifiers);
        private delegate void ModifyYoyoStatDelegate(Projectile proj, ref YoyoStatModifiers statModifiers, ref float value);
    }

    public class ModifyYoyoStatsThoriumCompatibility : ThoriumCompatibility
    {
        public override void Load()
        {
            var projectileExtrasTypeInfo = Assembly.GetType("ThoriumMod.Projectiles.ProjectileExtras");
            var yoyoAIMethodInfo = projectileExtrasTypeInfo?.GetMethod("YoyoAI", BindingFlags.Public | BindingFlags.Static);

            if (yoyoAIMethodInfo is null) return;

            MonoModHooks.Add(yoyoAIMethodInfo, (orig_YoyoAIMethod orig, int projIndex, float lifeTimeSeconds, float maxRange, float topSpeed, float rotateSpeed, Delegate _unknownAction, Delegate _unknownAction2) =>
            {
                var statModifiers = YoyoStatModifiers.Default;
                var lifeTime = lifeTimeSeconds * 60;
                ref var proj = ref Main.projectile[projIndex];

                IModifyYoyoStatsProjectile.GetModifyYoyoStats(proj, ref statModifiers);
                IModifyYoyoStatsProjectile.ModifyYoyoLifeTime(proj, ref statModifiers, ref lifeTime);
                IModifyYoyoStatsProjectile.ModifyYoyoMaxRange(proj, ref statModifiers, ref maxRange);

                orig(projIndex, lifeTime / 60, maxRange, topSpeed, rotateSpeed, _unknownAction, _unknownAction2);
            });
        }

        private delegate void orig_YoyoAIMethod(int projIndex, float lifeTimeSeconds, float maxRange, float topSpeed, float rotateSpeed, Delegate _unknownAction, Delegate _unknownAction2);
    }
}