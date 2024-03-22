using Microsoft.Xna.Framework;
using MonoMod.Cil;
using SPYoyoMod.Common.Configs;
using SPYoyoMod.Common.ModCompatibility;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Mono.Cecil.Cil.OpCodes;

namespace SPYoyoMod.Common
{
    public class YoyoUseStyleGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return lateInstantiation && item.IsYoyo();
        }

        public override void Load()
        {
            IL_Main.DrawProj_Inner += (il) =>
            {
                var c = new ILCursor(il);

                // Vector2 mountedCenter = Main.player[proj.owner].MountedCenter;

                // IL_0018: ldsfld       class Terraria.Player[] Terraria.Main::player
                // IL_001d: ldarg.1      // proj
                // IL_001e: ldfld int32 Terraria.Projectile::owner
                // IL_0023: ldelem.ref
                // IL_0024: callvirt instance valuetype[FNA]Microsoft.Xna.Framework.Vector2 Terraria.Player::get_MountedCenter()
                // IL_0029: stloc.2      // mountedCenter

                var mountedCenterIndex = -1;

                if (!c.TryGotoNext(MoveType.After,
                        i => i.MatchLdsfld(typeof(Main).GetField("player")),
                        i => i.MatchLdarg(1),
                        i => i.MatchLdfld<Projectile>("owner"),
                        i => i.MatchLdelemRef(),
                        i => i.MatchCallvirt<Player>("get_MountedCenter"),
                        i => i.MatchStloc(out mountedCenterIndex))) return;

                c.Emit(Ldarg_1);
                c.Emit(Ldloca, mountedCenterIndex);
                c.EmitDelegate<ModifyMountedCenterDelegate>(ModifyMountedCenter);
            };

            ModContent.GetInstance<ThoriumCompatibility>()?.AddILHook("Projectiles.ProjectileExtras", "DrawString", (il) =>
            {
                var c = new ILCursor(il);

                // Vector2 vector2_1 = Main.player[projectile.owner].MountedCenter;

                // IL_0008: ldsfld       class [tModLoader]Terraria.Player[] [tModLoader]Terraria.Main::player
                // IL_000d: ldloc.0      // projectile
                // IL_000e: ldfld int32[tModLoader]Terraria.Projectile::owner
                // IL_0013: ldelem.ref
                // IL_0014: callvirt instance valuetype[FNA]Microsoft.Xna.Framework.Vector2[tModLoader]Terraria.Player::get_MountedCenter()
                // IL_0019: stloc.1      // vector2_1

                var mountedCenterIndex = -1;
                var projIndex = -1;

                if (!c.TryGotoNext(MoveType.After,
                        i => i.MatchLdsfld(typeof(Main).GetField("player")),
                        i => i.MatchLdloc(out projIndex),
                        i => i.MatchLdfld<Projectile>("owner"),
                        i => i.MatchLdelemRef(),
                        i => i.MatchCallvirt<Player>("get_MountedCenter"),
                        i => i.MatchStloc(out mountedCenterIndex))) return;

                c.Emit(Ldloc, projIndex);
                c.Emit(Ldloca, mountedCenterIndex);
                c.EmitDelegate<ModifyMountedCenterDelegate>(ModifyMountedCenter);
            });
        }

        public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // Fix one-frame incorrect player direction
            position += Vector2.Normalize(velocity) * 2f;
        }

        public override void UseStyle(Item item, Player player, Rectangle heldItemFrame)
        {
            if (!item.useStyle.Equals(ItemUseStyleID.Shoot) || !ModContent.GetInstance<ServerSideConfig>().ReworkedYoyoUseStyle) return;

            /// [Old]

            // Conflict with HighFPSSupport mod...
            // float rotation = player.itemRotation * player.gravDir - 1.57079637f * player.direction;

            /// [New]

            var projIndex = -1; // Main yoyo proj index

            for (var index = 0; index < Main.projectile.Length; ++index)
            {
                ref var proj = ref Main.projectile[index];

                if (proj.type == item.shoot && proj.active && proj.owner == player.whoAmI)
                {
                    projIndex = proj.whoAmI;
                    break;
                }
            }

            if (projIndex < 0) return;

            var vectorFromPlayerToYoyo = Main.projectile[projIndex].Center - player.MountedCenter - GetMountedCenterOffset(player);
            var rotation = vectorFromPlayerToYoyo.ToRotation() * player.gravDir - 1.57079637f;

            /// ...

            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, rotation);
        }

        public static void ModifyMountedCenter(Projectile proj, ref Vector2 mountedCenter)
        {
            if (!proj.IsYoyo()) return;

            mountedCenter += GetMountedCenterOffset(Main.player[proj.owner]);
        }

        public static Vector2 GetMountedCenterOffset(Player player)
        {
            if (!ModContent.GetInstance<ServerSideConfig>().ReworkedYoyoUseStyle) return Vector2.Zero;
            return new(player.direction * -4f, player.gravDir >= 0f ? -4 : -10);
        }

        public delegate void ModifyMountedCenterDelegate(Projectile proj, ref Vector2 mountedCenter);
    }
}