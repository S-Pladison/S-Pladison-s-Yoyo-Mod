using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public sealed class YoyoUseStyle : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
            => lateInstantiation && item.IsYoyo();

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

                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldloca, mountedCenterIndex);
                c.EmitDelegate<ModifyMountedCenterDelegate>(ModifyMountedCenter);
            };
        }

        public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // Исправляет неверное направление игрока на первый кадр использования йо-йо
            position += Vector2.Normalize(velocity) * 2f;
        }

        public override void UseStyle(Item item, Player player, Rectangle heldItemFrame)
        {
            if (!item.useStyle.Equals(ItemUseStyleID.Shoot))
                return;

            /// [Old]

            // Конфликтует с модом "HighFPSSupport" ...
            // float rotation = player.itemRotation * player.gravDir - 1.57079637f * player.direction;

            /// [New]

            var projIndex = -1; // Индекс основного йо-йо

            for (var index = 0; index < Main.projectile.Length; ++index)
            {
                ref var proj = ref Main.projectile[index];

                if (proj.type == item.shoot && proj.active && proj.owner == player.whoAmI)
                {
                    projIndex = proj.whoAmI;
                    break;
                }
            }

            if (projIndex < 0)
                return;

            var vectorFromPlayerToYoyo = Main.projectile[projIndex].Center - player.MountedCenter - GetMountedCenterOffset(player);
            var rotation = vectorFromPlayerToYoyo.ToRotation() * player.gravDir - MathHelper.PiOver2;

            /// ...

            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, rotation);
        }

        private static void ModifyMountedCenter(Projectile proj, ref Vector2 mountedCenter)
        {
            if (!proj.IsYoyo())
                return;

            mountedCenter += GetMountedCenterOffset(proj.GetOwner());
        }

        private static Vector2 GetMountedCenterOffset(Player player)
            => new(player.direction * -4f, player.gravDir >= 0f ? -4 : -10);

        private delegate void ModifyMountedCenterDelegate(Projectile proj, ref Vector2 mountedCenter);
    }
}