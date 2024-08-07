using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Utils;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    public sealed class YoyoUseStyleGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
            => lateInstantiation && item.IsYoyo();

        public override void Load()
        {
            // Изменяем позицию, откуда нить йо-йо начинает отрисовываться
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

                if (!c.TryGotoNext(
                    MoveType.After,
                    i => i.MatchLdsfld(typeof(Main).GetField("player")),
                    i => i.MatchLdarg(1),
                    i => i.MatchLdfld<Projectile>("owner"),
                    i => i.MatchLdelemRef(),
                    i => i.MatchCallvirt<Player>("get_MountedCenter"),
                    i => i.MatchStloc(out mountedCenterIndex)))
                {

                    ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(YoyoUseStyleGlobalItem)}..{nameof(IL_Main.DrawProj_Inner)}\" failed...");
                    return;
                }

                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldloca, mountedCenterIndex);
                c.EmitDelegate<ModifyMountedCenterDelegate>(ModifyMountedCenter);
            };

            // Ну, по неизвестной мне причине, все снаряды йо-йо отрисовываются 2 раза...
            // 1 - При отрисовке самого снаряда
            // 2 - При отрисовки игрока (heldProj или тип того)
            // proj.hide = true в свою очередь убирает 1-ую отрисовку.
            // - Почему не на всех снарядах йо-йо?
            // Не хочу портить внешний вид йо-йо из других модов...
            // Они явно отрисованы так как им нужно.
            On_Main.DrawProjectiles += (orig, main) =>
            {
                var heldYoyoProjs = new List<(int, bool)>(4);

                foreach (var player in Main.ActivePlayers)
                {
                    if (player.heldProj < 0)
                        continue;

                    ref var proj = ref Main.projectile[player.heldProj];

                    if (!proj.IsYoyo() || proj.IsCounterweight())
                        continue;

                    if (!proj.IsVanilla() && !(proj.ModProjectile is not null && proj.ModProjectile.Mod is SPYoyoMod))
                        continue;

                    heldYoyoProjs.Add((proj.whoAmI, proj.hide));
                }

                foreach (var (projIndex, _) in heldYoyoProjs)
                {
                    ref var proj = ref Main.projectile[projIndex];
                    proj.hide = true;
                }

                orig(main);

                foreach (var (projIndex, projHide) in heldYoyoProjs)
                {
                    ref var proj = ref Main.projectile[projIndex];
                    proj.hide = projHide;
                }
            };
        }

        public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (!item.useStyle.Equals(ItemUseStyleID.Shoot))
                return;

            // Исправляет неверное направление игрока на первый кадр использования йо-йо
            position += velocity.SafeNormalize(velocity) * 2f;
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