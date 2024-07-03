using System;
using System.Linq;

namespace SPYoyoMod.Common
{
    /// <summary>
    /// Позволяет установить приоритет автоматической загрузки типов.
    /// Для загрузки у типа должен присутствовать <see cref="Terraria.ModLoader.AutoloadAttribute"/>.
    /// </summary>
    /// <param name="value">Значение приоритета. Чем больше значение, тем раньше загрузится тип.</param>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public sealed class LoadPriorityAttribute(sbyte value) : Attribute
    {
        private static readonly LoadPriorityAttribute Default = new(0);

        /// <summary>
        /// Значение приоритета. Чем больше значение, тем раньше загрузится тип.
        /// </summary>
        public readonly sbyte Value = value;

        public static LoadPriorityAttribute GetValue(Type type)
        {
            object[] all = type.GetCustomAttributes(typeof(LoadPriorityAttribute), true);
            var mostDerived = all.FirstOrDefault() as LoadPriorityAttribute;
            return mostDerived ?? Default;
        }
    }
}