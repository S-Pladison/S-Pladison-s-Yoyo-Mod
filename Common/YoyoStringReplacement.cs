using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Common.Graphics.Renderers;
using SPYoyoMod.Common.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    [Autoload(Side = ModSide.Client)]
    public sealed class YoyoStringReplacement : GlobalProjectile, IInitializableProjectile
    {
        private YoyoStringRenderer _stringRenderer;

        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
            => lateInstantiation && proj.IsYoyo();

        public override void Load()
        {
            // Переопределяем ванильный метод отрисовки нити
            IL_Main.DrawProj_DrawYoyoString += (il) =>
            {
                var cursor = new ILCursor(il);

                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate((Projectile proj, Vector2 mountedCenter) =>
                {
                    if (!proj.IsYoyo() || !proj.TryGetGlobalProjectile(out YoyoStringReplacement globalProj) || globalProj._stringRenderer is null)
                        return;

                    ref var renderer = ref globalProj._stringRenderer;

                    renderer.SetStartPosition(mountedCenter + proj.GetOwner()?.gfxOffY * Vector2.UnitY ?? Vector2.Zero);
                    renderer.Render();

                    if (proj.GetOwner().heldProj != proj.whoAmI)
                        return;

                    // Отрисовка нити для ванильных йо-йо и йо-йо из этого мода отличается от отрисовки йо-йо из других модов.
                    // - Почему?
                    // В YoyoUseStyle.cs есть логика, которая убирает проблему второй отрисовки ванильных йо-йо и йо-йо из этого мода, но
                    // для остальных все остается как прежде.
                    // Поэтому, фигачим отрисовку для них еще раз :p
                    if (!proj.IsVanilla() && !(proj.ModProjectile is not null && proj.ModProjectile.Mod is SPYoyoMod))
                        return;

                    renderer.Render();
                });

                cursor.Emit(OpCodes.Ret);
            };
        }

        public void Initialize(Projectile proj)
        {
            _stringRenderer = new YoyoStringRenderer(proj, new IDrawYoyoStringSegment.Vanilla());
        }
    }
}