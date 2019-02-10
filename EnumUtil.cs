using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DotStd
{
    public static class EnumUtil
    {
        // Util helper to pull metadata from enums

        public static string GetEnumDescription(Enum value)
        {
            // Get the Metadata Description tag string for an enum (if it has one) else default to the enums name.
            // e.g. [Description("SWE")] using System.ComponentModel; DescriptionAttribute
            if (value == null)
            {
                return "";
            }
            Type type = value.GetType();
            string name = value.ToString();
            MemberInfo[] memInfo = type.GetMember(name);   // could use GetField() for FieldInfo ?
            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            return name;   // default to using the enums name. like: Enum.GetName(type, en);
        }

        /// <summary>
        /// Gets the <see cref="DescriptionAttribute"/> of an <see cref="Enum"/> type value.
        /// </summary>
        /// <param name="value">The <see cref="Enum"/> type value.</param>
        /// <returns>A string containing the text of the <see cref="DescriptionAttribute"/>.</returns>
        public static string ToDescription(this Enum value)
        {
            // If this enum has a meta data description then return it. else just the string version of the enum
            return GetEnumDescription(value);
        }

        public static int ToInt(this Enum value)
        {
            // when casting to int seems strange.
            // https://stackoverflow.com/questions/943398/get-int-value-from-enum-in-c-sharp
            return Convert.ToInt32(value);
        }

        /// <summary>
        ///  Converts the <see cref="Enum"/> type to an <see cref="IList"/> compatible object.
        /// </summary>
        /// <param name="enumType">The <see cref="Enum"/> type.</param>
        /// <returns>An <see cref="IList"/> containing the enumerated type value and description.</returns>
        public static IEnumerable<TupleIdValue> ToEnumList(this Type enumType)
        {
            ValidState.ThrowIfNull(enumType, enumType.Name);

            if (!enumType.IsEnum)
            {
                throw new ArgumentException(enumType.Name);
            }

            var list = new List<TupleIdValue>();
            Array enumValues = Enum.GetValues(enumType);

            foreach (Enum value in enumValues)
            {
                list.Add(new TupleIdValue(value.ToInt(), GetEnumDescription(value)));
            }

            return list;
        }

        public static T ParseEnum<T>(string value) // where T : System.Enum
        {
            // convert a string to enum T if possible.
            // look up a string and compare it to the declared enum values.
            try
            {
                return (T)Enum.Parse(typeof(T), value, true);   // ignore case.
            }
            catch
            {
            }
            return default(T);  // always 0
        }

        public static T ParseEnum2<T>(string value) // where T : System.Enum
        {
            // a much more forgiving version of ParseEnum, ignore case.
            // check if its numeric, use GetDescription ??
            try
            {
                // is numeric ?
                // Try desc?
                return (T) Enum.Parse(typeof(T), value, true);   // ignore case.
            }
            catch
            {
            }
            return default(T);  // always 0
        }
    }
}
