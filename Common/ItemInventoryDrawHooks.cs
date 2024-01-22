using MonoMod.Cil;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using static Mono.Cecil.Cil.OpCodes;

namespace SPYoyoMod.Common
{
    [Autoload(Side = ModSide.Client)]
    public sealed class ItemInventoryDrawHooks : ILoadable
    {
        void ILoadable.Load(Mod mod)
        {
            IL_ItemSlot.DrawItemIcon += (il) =>
            {
                var c = new ILCursor(il);

                // float finalDrawScale;
                // ItemSlot.DrawItem_GetColorAndScale(item, scale, ref environmentColor, sizeLimit, ref frame, out itemLight, out finalDrawScale);

                // IL_005a: ldarg.0 // item
                // IL_005b: ldarg.s scale
                // IL_005d: ldarga.s environmentColor
                // IL_005f: ldarg.s sizeLimit
                // IL_0061: ldloca.s frame
                // IL_0063: ldloca.s itemLight
                // IL_0065: ldloca.s finalDrawScale
                // IL_0067: call void ItemSlot::DrawItem_GetColorAndScale(...)

                int finalDrawScaleIndex = -1;

                if (!c.TryGotoNext(MoveType.After,
                        i => i.MatchLdarg0(),
                        i => i.MatchLdarg(out _),
                        i => i.MatchLdarga(out _),
                        i => i.MatchLdarg(out _),
                        i => i.MatchLdloca(out _),
                        i => i.MatchLdloca(out _),
                        i => i.MatchLdloca(out finalDrawScaleIndex),
                        i => i.MatchCall(typeof(ItemSlot).GetMethod(nameof(ItemSlot.DrawItem_GetColorAndScale), BindingFlags.Public | BindingFlags.Static)))) return;

                c.Emit(Ldarg_0);
                c.Emit(Ldloca, finalDrawScaleIndex);
                c.EmitDelegate<ModifyFinalDrawScaleDelegate>(ModifyFinalDrawScale);
            };
        }

        void ILoadable.Unload() { }

        public static void ModifyFinalDrawScale(Item item, ref float finalDrawScale)
        {
            var scaleMult = ModSets.Items.InventoryDrawScaleMultiplier[item.type];

            if (scaleMult.HasValue)
            {
                finalDrawScale *= scaleMult.Value;
            }
        }

        public delegate void ModifyFinalDrawScaleDelegate(Item item, ref float finalDrawScale);
    }
}