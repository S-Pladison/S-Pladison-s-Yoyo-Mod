using Terraria.ModLoader;
using Terraria;
using MonoMod.Cil;
using Terraria.ID;
using Terraria.ModLoader.Core;
using System;
using static Mono.Cecil.Cil.OpCodes;

namespace SPYoyoMod.Common
{
    public interface IModifyYoyoStats
    {
        public static readonly GlobalHookList<GlobalProjectile> Hook;

        static IModifyYoyoStats()
        {
            Hook = ProjectileLoader.AddModHook(new GlobalHookList<GlobalProjectile>(typeof(IModifyYoyoStats).GetMethod(nameof(ModifyYoyoStats))));
        }

        void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers);

        private class HookImplementation : ILoadable
        {
            // No comments please...
            private YoyoStatModifiers currentStatModifiers;

            public void Load(Mod mod)
            {
                IL_Projectile.AI_099_2 += (il) =>
                {
                    var c = new ILCursor(il);

                    c.Emit(Ldarg_0);
                    c.EmitDelegate<Action<Projectile>>(proj =>
                    {
                        currentStatModifiers = YoyoStatModifiers.Default;

                        (proj.ModProjectile as IModifyYoyoStats)?.ModifyYoyoStats(proj, ref currentStatModifiers);

                        foreach (IModifyYoyoStats g in Hook.Enumerate(proj))
                        {
                            g.ModifyYoyoStats(proj, ref currentStatModifiers);
                        }
                    });

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
                    c.Emit(Ldloca, num2Index);
                    c.EmitDelegate<ModifyYoyoStatDelegate>(ModifyYoyoLifeTime);

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
                    c.Emit(Ldloca, num10Index);
                    c.EmitDelegate<ModifyYoyoStatDelegate>(ModifyYoyoMaxRange);
                };
            }

            public void ModifyYoyoLifeTime(Projectile proj, ref float lifeTime)
            {
                lifeTime = currentStatModifiers.LifeTime.ApplyTo(lifeTime);
            }

            public void ModifyYoyoMaxRange(Projectile proj, ref float maxRange)
            {
                maxRange = currentStatModifiers.MaxRange.ApplyTo(maxRange);
            }

            public void Unload() { }

            private delegate void ModifyYoyoStatDelegate(Projectile proj, ref float value);
        }
    }
}