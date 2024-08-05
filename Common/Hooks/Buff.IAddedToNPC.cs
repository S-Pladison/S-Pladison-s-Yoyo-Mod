using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Utils;
using System;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Hooks
{
    /// <summary>
    /// Позволяет баффу знать, когда он был добавлен к NPC.
    /// <br/>Интерфейс относится к следующим классам: <see cref="ModBuff"/> и <see cref="GlobalBuff"/>
    /// </summary>
    public interface IAddedToNPCBuff
    {
        /// <summary>
        /// Позволяет баффу знать, когда он был добавлен к NPC.
        /// </summary>
        void OnAddToNPC(int buffType, int buffIndex, NPC npc);
    }

    internal sealed class AddedToNPCBuffImplementation : ILoadable
    {
        private Action<int, int, NPC>[] _hook;

        public void Load(Mod mod)
        {
            ModEvents.OnPostSetupContent += () =>
            {
                _hook = BuffUtils.GetGlobalHook<Action<int, int, NPC>>(g => (g as IAddedToNPCBuff).OnAddToNPC);
            };

            IL_NPC.AddBuff += (il) =>
            {
                var cursor = new ILCursor(il);

                // Добавляем новую локальную переменную для сохранения типа предыдущего баффа
                var oldbuffTypeIndex = il.Body.Variables.Count;
                il.Body.Variables.Add(new VariableDefinition(cursor.Context.Import(typeof(int))));

                // Переходим в конец функции
                cursor.Index = cursor.Instrs.Count;
                if (!cursor.TryGotoPrev(MoveType.Before, i => i.MatchRet()))
                {
                    ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(AddedToNPCBuffImplementation)}..{nameof(IL_NPC.AddBuff)}\" failed...");
                    return;
                }

                // buffType[num] = type;

                // IL_00fc: ldarg.0
                // IL_00fd: ldfld int32[] Terraria.NPC::buffType
                // IL_0102: ldloc.0
                // IL_0103: ldarg.1
                // IL_0104: stelem.i4

                // Ищем с конца нужную нам строчку
                if (!cursor.TryGotoPrev(
                    MoveType.Before,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<NPC>("buffType"),
                    i => i.MatchLdloc(0),
                    i => i.MatchLdarg(1),
                    i => i.MatchStelemI4()))
                {
                    ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(AddedToNPCBuffImplementation)}..{nameof(IL_NPC.AddBuff)}\" failed...");
                    return;
                }

                // Помещаем значение типа предыдущего баффа в нашу локальную переменную
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(NPC).GetField("buffType"));
                cursor.Emit(OpCodes.Ldloc_0);
                cursor.Emit(OpCodes.Ldelem_I4);
                cursor.Emit(OpCodes.Stloc, oldbuffTypeIndex);

                // Снова переходим в конец функции
                cursor.Index = cursor.Instrs.Count;
                if (!cursor.TryGotoPrev(MoveType.Before, i => i.MatchRet()))
                {
                    ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(AddedToNPCBuffImplementation)}..{nameof(IL_NPC.AddBuff)}\" failed...");
                    return;
                }

                // Вставляем наш код в конец функции
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Ldloc_0);
                cursor.Emit(OpCodes.Ldloc, oldbuffTypeIndex);
                cursor.EmitDelegate((NPC npc, int buffType, int buffIndex, int oldBuffType) =>
                {
                    if (oldBuffType == buffType)
                        return;

                    if (BuffUtils.IsModBuff(buffType))
                        (BuffLoader.GetBuff(buffType) as IAddedToNPCBuff)?.OnAddToNPC(buffType, buffIndex, npc);

                    foreach (var action in _hook)
                        action(buffType, buffIndex, npc);
                });
            };
        }

        public void Unload()
        {
            _hook = null;
        }
    }
}