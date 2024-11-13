using System;
using System.Collections.Generic;

namespace SPYoyoMod.Core.ModSupport
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public sealed class ModInternalNameAttribute(string value) : Attribute
    {
        public readonly string Value = value;

        public static bool TryGetValues(Type type, out IReadOnlyList<string> values)
        {
            var attributes = type.GetCustomAttributes(typeof(ModInternalNameAttribute), true);

            if (attributes.Length == 0)
            {
                values = null;
                return false;
            }

            var internalNames = new List<string>(attributes.Length);

            foreach (var attribute in attributes)
                internalNames.Add((attribute as ModInternalNameAttribute).Value);

            values = internalNames.AsReadOnly();
            return true;
        }
    }
}