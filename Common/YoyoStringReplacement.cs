using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Common.Graphics.Renderers;
using SPYoyoMod.Common.Hooks;
using SPYoyoMod.Common.ModSupport;
using SPYoyoMod.Utils;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common
{
    [Autoload(Side = ModSide.Client)]
    public sealed class YoyoStringReplacementGlobalProjectile : GlobalProjectile, IInitializableProjectile
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
                    if (!proj.IsYoyo() || !proj.TryGetGlobalProjectile(out YoyoStringReplacementGlobalProjectile globalProj) || globalProj._stringRenderer is null)
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

            // Thorium имеет собственную функцию отрисовки нитей для своих йо-йо...
            // Так как рисуются одновременно обе нити, можно спокойно прекратить отрисовку одной из.
            // Но из-за отсутствия в Thorium смещения (из YoyoUseStyle.cs), прекращаем отрисовку именно у них :p
            if (ThoriumModSupport.IsModLoaded)
            {
                try
                {
                    var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
                    var methodInfo = ThoriumModSupport.Code.GetType("ThoriumMod.Projectiles.ProjectileExtras").GetMethod("DrawString", flags) ?? throw new Exception();

                    MonoModHooks.Add(methodInfo, (orig_ThoriumModDrawString orig, int index, Vector2 to, Vector2 from, int stringColor, bool actuallyYoyo) =>
                    {
                        // Просто не вызываем orig(...); и все :)
                    });
                }
                catch (Exception)
                {
                    Mod.Logger.Warn($"Hook \"{nameof(YoyoStringReplacementGlobalProjectile)}..{nameof(ThoriumModSupport)}\" failed...");
                }
            }
        }

        public void Initialize(Projectile proj)
        {
            _stringRenderer = new YoyoStringRenderer(proj, new IDrawYoyoStringSegments.Vanilla());
        }

        private delegate void orig_ThoriumModDrawString(int index, Vector2 to, Vector2 from, int stringColor, bool actuallyYoyo);
    }
}