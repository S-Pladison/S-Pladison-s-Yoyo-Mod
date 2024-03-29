﻿using MonoMod.Cil;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.ModCompatibility;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Rendering;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static Mono.Cecil.Cil.OpCodes;

namespace SPYoyoMod.Content.Items.Vanilla.Accessories
{
    public class StringsItem : GlobalItem
    {
        public static LocalizedText Tooltip { get; private set; }

        public override void Load()
        {
            Tooltip = Language.GetOrRegister("Mods.SPYoyoMod.VanillaItems.StringItem.Tooltip");
        }

        public override void Unload()
        {
            Tooltip = null;
        }

        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return item.type >= ItemID.RedString && item.type <= ItemID.BlackString;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            var line = TooltipUtils.FindDescriptionLast(tooltips);

            if (line is not null)
                line.Text = Tooltip.Value;
        }
    }

    public class StringsGlobalProjectile : GlobalProjectile, IModifyYoyoStatsProjectile
    {
        public override void Load()
        {
            IL_Projectile.AI_099_1 += (il) =>
            {
                var c = new ILCursor(il);

                // if (yoyoString)
                // {
                //     num5 += (float)((double)num5 * 0.25 + 10.0);
                // }

                // IL_00ee: ldloc.1      // num5
                // IL_00ef: ldloc.1      // num5
                // IL_00f0: ldc.r4       0.25
                // IL_00f5: mul
                // IL_00f6: ldc.r4       10
                // IL_00fb: add
                // IL_00fc: add
                // IL_00fd: stloc.1      // num5

                var num5Index = -1;

                if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdloc(out num5Index),
                    i => i.MatchLdloc(num5Index),
                    i => i.MatchLdcR4(0.25f),
                    i => i.MatchMul(),
                    i => i.MatchLdcR4(10f),
                    i => i.MatchAdd(),
                    i => i.MatchAdd(),
                    i => i.MatchStloc(num5Index))) return;

                c.Emit(Ldloca, num5Index);
                c.EmitDelegate<RemoveStringBonusDelegate>(RemoveCounterweightStringBonus);
            };

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
                    i => i.MatchStloc(num10Index))) return;

                c.Emit(Ldloca, num10Index);
                c.EmitDelegate<RemoveStringBonusDelegate>(RemoveYoyoStringBonus);
            };

            ModContent.GetInstance<ThoriumCompatibility>()?.AddILHook("Projectiles.ProjectileExtras", "YoyoAI", (il) =>
            {
                var c = new ILCursor(il);

                // if (player.yoyoString)
                // {
                //     num3 = (float)((double)num3 * 1.25 + 30.0);
                // }

                var num3Index = -1;

                if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdloc(out num3Index),
                    i => i.MatchLdcR4(1.25f),
                    i => i.MatchMul(),
                    i => i.MatchLdcR4(30f),
                    i => i.MatchAdd(),
                    i => i.MatchStloc(num3Index))) return;

                c.Emit(Ldloca, num3Index);
                c.EmitDelegate<RemoveStringBonusDelegate>(RemoveYoyoStringBonus);
            });
        }

        public void ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            var owner = Main.player[proj.owner];

            if (!owner.yoyoString) return;

            statModifiers.MaxRange.Flat += 16 * 4;
        }

        public static void RemoveYoyoStringBonus(ref float length)
        {
            length = (length - 30) / 1.25f;
        }

        public static void RemoveCounterweightStringBonus(ref float length)
        {
            length = (length - 10) / 1.25f;
        }

        public delegate void RemoveStringBonusDelegate(ref float value);
    }
}