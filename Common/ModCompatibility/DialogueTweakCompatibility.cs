using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.ModCompatibility
{
    public sealed class DialogueTweakCompatibility : ModCompatibility
    {
        public override string ModName => "DialogueTweak";

        public void AddButton(int npcType, Func<string> buttonText, string iconTexturePath, Action hoverCallback, Func<bool> availability = null, Func<Rectangle> frame = null, Func<float> customTextOffset = null)
        {
            AddButton(new List<int> { npcType }, buttonText, () => iconTexturePath, hoverCallback, availability, frame, customTextOffset);
        }

        public void AddButton(List<int> npcType, Func<string> buttonText, Func<string> iconTexturePath, Action hoverCallback, Func<bool> availability = null, Func<Rectangle> frame = null, Func<float> customTextOffset = null)
        {
            if (!IsModLoaded) return;

            availability ??= () => true;

            if (Mod.Call("AddButton", npcType, buttonText, iconTexturePath, hoverCallback, availability, frame, customTextOffset) is not bool value || !value)
                ModContent.GetInstance<SPYoyoMod>().Logger.Error($"Error:[Failed to call 'AddButton'] Mod:[{ModName}] NPCTypes:[{string.Join(",", npcType)}]");
        }
    }
}