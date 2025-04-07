using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace SPYoyoMod.Core.ModSupport
{
    public sealed class DialogueTweakSupport : ModSupportSystem<DialogueTweakSupport>
    {
        private DialogueTweakSupport() : base(internalName: "DialogueTweak") { }

        /// <summary>
        /// Добавляет кнопку для определенного NPC.
        /// </summary>
        /// <param name="npcType">Тип NPC, к которому добавляется кнопка.</param>
        /// <param name="buttonText">Отображаемый текст кнопки. Необходимо передать функцию типа <see cref="Func{TResult}"/>. Можно использовать <see cref="Language.GetTextValue"/> или что-нибудь еще.</param>
        /// <param name="iconTexturePath">Путь к текстурке иконки. Если вернуть пустую строку или <see langword="null"/>, то иконки не будет.</param>
        /// <param name="hoverCallback">Действие, вызываемая при наведении на кнопку. Используйте ее для определения поведения того, была ли прожата кнопка.</param>
        /// <param name="availability">Доступна ли эта кнопка.</param>
        /// <param name="frame">Область текстуры иконки, которая будет отображаться.</param>
        /// <param name="customTextOffset">Расстояние от левой стороны области, содержащей текст, до левой стороны кнопки.</param>
        public static void AddButton(int npcType, Func<string> buttonText, string iconTexturePath, Action hoverCallback, Func<bool> availability = null, Func<Rectangle> frame = null, Func<float> customTextOffset = null)
        {
            AddButton([npcType], buttonText, () => iconTexturePath, hoverCallback, availability, frame, customTextOffset);
        }

        /// <summary>
        /// Добавляет кнопку для определенных NPC.
        /// </summary>
        /// <param name="npcType">Список типов NPC, к которым добавляется кнопка.</param>
        /// <param name="buttonText">Отображаемый текст кнопки. Необходимо передать функцию типа <see cref="Func{TResult}"/>. Можно использовать <see cref="Language.GetTextValue"/> или что-нибудь еще.</param>
        /// <param name="iconTexturePath">Путь к текстурке иконки. Если вернуть пустую строку или <see langword="null"/>, то иконки не будет.</param>
        /// <param name="hoverCallback">Действие, вызываемая при наведении на кнопку. Используйте ее для определения поведения того, была ли прожата кнопка.</param>
        /// <param name="availability">Доступна ли эта кнопка.</param>
        /// <param name="frame">Область текстуры иконки, которая будет отображаться.</param>
        /// <param name="customTextOffset">Расстояние от левой стороны области, содержащей текст, до левой стороны кнопки.</param>
        public static void AddButton(List<int> npcType, Func<string> buttonText, Func<string> iconTexturePath, Action hoverCallback, Func<bool> availability = null, Func<Rectangle> frame = null, Func<float> customTextOffset = null)
        {
            availability ??= () => true;

            if (Call("AddButton", npcType, buttonText, iconTexturePath, hoverCallback, availability, frame, customTextOffset) is bool result && !result)
                ModContent.GetInstance<SPYoyoMod>().Logger.Error($"Error:[Failed to call 'AddButton'] Mod:[{Instance.Name}] NPCTypes:[{string.Join(",", npcType)}]");
        }
    }
}