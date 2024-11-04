using System;
using System.Collections.Generic;
using System.Linq;

namespace SPYoyoMod.Utils
{
    public static class EnumUtils
    {
        /// <summary>
        /// Получить коллекцию всех доступных вариантов данного перечисления.
        /// </summary>
        public static IEnumerable<T> GetVariants<T>() where T : Enum
        {
            var values = Enum.GetValues(typeof(T)).Cast<int>().ToArray();

            if (typeof(T).GetCustomAttributes(typeof(FlagsAttribute), false).Length == 0)
                return Enum.GetValues(typeof(T)).Cast<T>();

            var valuesInverted = values.Select(v => ~v).ToArray();
            int max = 0;

            for (int i = 0; i < values.Length; i++)
                max |= values[i];

            var result = new List<T>();

            for (int i = 0; i <= max; i++)
            {
                int unaccountedBits = i;

                for (int j = 0; j < valuesInverted.Length; j++)
                {
                    unaccountedBits &= valuesInverted[j];

                    if (unaccountedBits == 0)
                    {
                        result.Add((T)(object)i);
                        break;
                    }
                }
            }

            try
            {
                if (string.IsNullOrEmpty(Enum.GetName(typeof(T), (T)(object)0)))
                    result.Remove((T)(object)0);
            }
            catch
            {
                result.Remove((T)(object)0);
            }

            return result;
        }
    }
}
