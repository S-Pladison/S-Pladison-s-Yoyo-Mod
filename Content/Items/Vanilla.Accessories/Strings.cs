using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Common.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Accessories
{
    public sealed class StringProjectile : GlobalProjectile, IModifyYoyoStatsProjectile
    {
        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
                => lateInstantiation && proj.IsYoyo();
        
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
                // IL_064B: stloc.s   num10

                var num10Index = -1;

                if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdloc(out num10Index),
                    i => i.MatchLdcR4(1.25f),
                    i => i.MatchMul(),
                    i => i.MatchLdcR4(30f),
                    i => i.MatchAdd(),
                    i => i.MatchStloc(num10Index))) {

                    ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(StringProjectile)}..{nameof(IL_Projectile.AI_099_2)}\" failed...");
                    return;
                }

                c.Emit(OpCodes.Ldloca, num10Index);
                c.EmitDelegate(RemoveYoyoStringBonus);
            };
        }

        public void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            if (!proj.GetOwner().yoyoString)
                return;
            
            statModifiers.MaxRange.Flat += ProjectileID.Sets.YoyosMaximumRange[proj.type] * 1.25f + 30f - ProjectileID.Sets.YoyosMaximumRange[proj.type];
        }

        private static void RemoveYoyoStringBonus(ref float length)
        {
            length = (length - 30.0f) / 1.25f;
        }
    }
}