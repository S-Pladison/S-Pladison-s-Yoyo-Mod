using SPYoyoMod.Utils;
using System;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Hooks
{
    /// <summary>
    /// Позволяет баффу знать, когда он был удален с NPC.
    /// <br/>Интерфейс относится к следующим классам: <see cref="ModBuff"/> и <see cref="GlobalBuff"/>
    /// </summary>
    public interface IDeletedFromNPCBuff
    {
        /// <summary>
        /// Позволяет баффу знать, когда он был удален с NPC.
        /// </summary>
        void OnDeleteFromNPC(int buffType, int buffIndex, NPC npc);
    }

    internal sealed class DeletedFromNPCBuffImplementation : ILoadable
    {
        private Action<int, int, NPC>[] _hook;

        public void Load(Mod mod)
        {
            ModEvents.OnPostSetupContent += () =>
            {
                _hook = BuffUtils.GetGlobalHook<Action<int, int, NPC>>(g => (g as IDeletedFromNPCBuff).OnDeleteFromNPC);
            };

            On_NPC.DelBuff += (orig, npc, buffIndex) =>
            {
                var buffType = npc.buffType[buffIndex];

                if (buffType > 0)
                {
                    if (BuffUtils.IsModBuff(buffType))
                        (BuffLoader.GetBuff(buffType) as IDeletedFromNPCBuff)?.OnDeleteFromNPC(buffType, buffIndex, npc);

                    foreach (var action in _hook)
                        action(buffType, buffIndex, npc);
                }

                orig(npc, buffIndex);
            };
        }

        public void Unload()
        {
            _hook = null;
        }
    }
}