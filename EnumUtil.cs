using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DotStd
{
    /// <summary>
    /// Utility helper to pull metadata from enums
    /// </summary>
    public static class EnumUtil
    {
        /// <summary>
        /// Is value a defined enum member value ?
        /// like Enum.IsDefined() ? 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsDefined(Enum value)
        {
            if (value == null)
                return false;
            Type type = value.GetType();
            if (type == null)
                return false;
            string name = value.ToString();
            if (string.IsNullOrWhiteSpace(name))
                return false;
            if (StringUtil.IsNumeric2(name))    // will create a fake member if its a number ! block that.
                return false;
            return type.GetMember(name) != null;
        }

        public static bool HasAttr(object[] attrs)
        {
            // IEnumerable<Attribute>
            return attrs != null && attrs.Length > 0;
        }

        public static string GetValName(Enum value)
        {
            // get a string with the int and name for the enum value.
            if (IsDefined(value))
            {
                return $"{value.ToInt()} ({value.ToString()})";
            }
            return value.ToString();
        }

        public static T GetEnum<T>(int value) where T : Enum
        {
            // Convert int to Enum.
            return (T)(object)value;
        }

        public static string GetValName<T>(int value) where T : Enum
        {
            // Get a string name for the enum value (else int)
            return GetValName(GetEnum<T>(value));
        }

        public static string GetValName<T>(int? value) where T : Enum
        {
            // Get a string name for the enum value (or null)
            if (value == null)
                return "";
            return GetValName<T>(value.Value);
        }

        public static string? GetDescription(MemberInfo[] memInfo)
        {
            // Get value for DescriptionAttribute

            if (memInfo == null || memInfo.Length <= 0)
                return null;
            object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (!HasAttr(attrs))
                return null;
            return ((DescriptionAttribute)attrs[0]).Description;
        }

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
            string? desc = GetDescription(type.GetMember(name));   // could use GetField() for FieldInfo ?
            return desc ?? name;   // default to using the enums name. like: Enum.GetName(type, en);
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
            // similar to Html.GetEnumSelectList<Type>()

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

        public static bool IsMatch<T>(string? value) where T : struct  // where T : System.Enum
        {
            T result;
            return Enum.TryParse<T>(value, true, out result);
        }

        public static T ParseEnum<T>(string value) where T : struct  // where T : System.Enum
        {
            // convert a string to enum T if possible.
            // look up a string and compare it to the declared enum values.
            T result;
            if (Enum.TryParse<T>(value, true, out result))  // ignore case.
                return result;
            return default(T);  // always 0
        }

        /// <summary>
        /// a much more forgiving version of ParseEnum, ignore case.
        /// check if its numeric, use GetDescription ??
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T ParseEnum2<T>(string value) where T : struct  // where T : System.Enum
        {
            try
            {
                // is numeric ?
                // Try desc?
                return (T)Enum.Parse(typeof(T), value, true);   // ignore case.
            }
            catch
            {
            }
            return default(T);  // always 0
        }
    }
}
