using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Common.Graphics;
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

                    if (!proj.hide)
                        return;
                    
                    // Хех... Для скрытых снарядов (для йо-йо из этого мода и измененных ванильных йо-йо)
                    // нужно отрисовать нить еще раз, чтобы она соответствовала *старому* ванильному стилю и другим модовым йо-йо...
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