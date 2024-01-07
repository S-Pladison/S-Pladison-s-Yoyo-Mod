using MonoMod.Cil;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Utils.DataStructures;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Mono.Cecil.Cil.OpCodes;

namespace SPYoyoMod.Content.Items.Vanilla.Accessories
{
    public class StringsGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation) { return item.type >= ItemID.RedString && item.type <= ItemID.BlackString; }
    }

    public class StringsGlobalProjectile : GlobalProjectile, IModifyYoyoStatsProjectile
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

                c.Emit(Ldarg_0);
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

        private static void RemoveStringBonus(Projectile proj, ref float length)
        {
            length = (length - 30) / 1.25f;
        }

        private delegate void RemoveStringBonusDelegate(Projectile proj, ref float value);
    }
}