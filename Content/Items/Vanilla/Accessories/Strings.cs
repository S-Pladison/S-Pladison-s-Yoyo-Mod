using MonoMod.Cil;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.ModCompatibility;
using SPYoyoMod.Utils.DataStructures;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Mono.Cecil.Cil.OpCodes;

namespace SPYoyoMod.Content.Items.Vanilla.Accessories
{
    public class StringsItem : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation) { return item.type >= ItemID.RedString && item.type <= ItemID.BlackString; }
    }

    public class StringsProjectile : GlobalProjectile, IModifyYoyoStatsProjectile
    {
        public override void Load()
        {
            IL_Projectile.AI_099_2 += (il) =>
            {
                var c = new ILCursor(il);

                // if (yoyoString)
                // {
                //     num7 = num7 * 1.25f + 30f;
                // }

                // IL_063D: ldloc.s   num10
                // IL_063F: ldc.r4    1.25
                // IL_0644: mul
                // IL_0645: ldc.r4    30
                // IL_064A: add
                // IL_064B: stloc.s num10

                int num10Index = -1;

                if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdloc(out num10Index),
                    i => i.MatchLdcR4(1.25f),
                    i => i.MatchMul(),
                    i => i.MatchLdcR4(30f),
                    i => i.MatchAdd(),
                    i => i.MatchStloc(num10Index))) return;

                c.Emit(Ldloca, num10Index);
                c.EmitDelegate<RemoveStringBonusDelegate>(RemoveStringBonus);
            };
        }

        public void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            var owner = Main.player[proj.owner];

            if (!owner.yoyoString) return;

            statModifiers.MaxRange.Flat += 16 * 3;
        }

        public static void RemoveStringBonus(ref float length)
        {
            length = (length - 30) / 1.25f;
        }

        public delegate void RemoveStringBonusDelegate(ref float value);
    }

    public class StringsThoriumCompatibility : ThoriumCompatibility
    {
        public override void Load()
        {
            var projectileExtrasTypeInfo = Assembly.GetType("ThoriumMod.Projectiles.ProjectileExtras");
            var yoyoAIMethodInfo = projectileExtrasTypeInfo?.GetMethod("YoyoAI", BindingFlags.Public | BindingFlags.Static);

            if (yoyoAIMethodInfo is null) return;

            MonoModHooks.Modify(yoyoAIMethodInfo, (il) =>
            {
                var c = new ILCursor(il);

                // if (player.yoyoString)
                // {
                //     num3 = (float)((double)num3 * 1.25 + 30.0);
                // }

                int num3Index = -1;

                if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdloc(out num3Index),
                    i => i.MatchLdcR4(1.25f),
                    i => i.MatchMul(),
                    i => i.MatchLdcR4(30f),
                    i => i.MatchAdd(),
                    i => i.MatchStloc(num3Index))) return;

                c.Emit(Ldloca, num3Index);
                c.EmitDelegate<StringsProjectile.RemoveStringBonusDelegate>(StringsProjectile.RemoveStringBonus);
            });
        }
    }
}