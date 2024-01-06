using Microsoft.Xna.Framework;
using MonoMod.Cil;
using SPYoyoMod.Common.Configs;
using SPYoyoMod.Utils.Extensions;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Mono.Cecil.Cil.OpCodes;

namespace SPYoyoMod.Common
{
    public class YoyoUseStyleGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation) { return item.IsYoyo(); }

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

                int mountedCenterIndex = -1;

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
        }

        public void ModifyMountedCenter(Projectile proj, ref Vector2 mountedCenter)
        {
            if (!proj.IsYoyo() || !ModContent.GetInstance<ClientSideConfig>().ReworkedYoyoUseStyle) return;

            mountedCenter += GetMountedCenterOffset(Main.player[proj.owner]);
        }

        public override void UseStyle(Item item, Player player, Rectangle heldItemFrame)
        {
            if (!item.useStyle.Equals(ItemUseStyleID.Shoot) || !ModContent.GetInstance<ClientSideConfig>().ReworkedYoyoUseStyle) return;

            float rotation = player.itemRotation * player.gravDir - 1.57079637f * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, rotation);
        }

        private static Vector2 GetMountedCenterOffset(Player player)
        {
            return new(player.direction * -4f, player.gravDir >= 0f ? -4 : -10);
        }

        private delegate void ModifyMountedCenterDelegate(Projectile proj, ref Vector2 mountedCenter);
    }
}