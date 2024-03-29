﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DotStd
{
    /// <summary>
    /// Convert from some (unknown) type object to a known type.
    /// Data type converter. usually for database derived objects. Deal with null and DBNull?
    /// Similar to System.Convert.To*() or System.Convert.ChangeType() but more forgiving.
    /// NOTE: NEVER allow untrusted sources to serialize to object/any types. This is an injection attack!
    /// </summary>
    public static class Converter
    {
        public const char kMinus2 = '−';      // Weird alternate char sometimes used as minus. '-'

        /// <summary>
        /// like o.GetType() but is decided at compile time and ignores null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputObject"></param>
        /// <returns></returns>
        public static Type GetCompileTimeType<T>(this T inputObject)
        {
            return typeof(T);
        }

        /// <summary>
        /// A forgiving conversion to bool
        /// </summary>
        /// <param name="o"></param>
        /// <param name="defVal">false</param>
        /// <returns></returns>
        public static bool ToBool(object? o, bool defVal = false)
        {
            if (ValidState.IsNull(o))
                return defVal;

            Type type = o.GetType();
            if (type == typeof(bool))     // faster convert
                return (bool)o;
            if (type == typeof(int))     // faster convert
                return ((int)o) != 0;
            if (type == typeof(bool?))     // faster convert
                return ((bool?)o) ?? defVal;
            if (type == typeof(System.Enum))     // don't convert this to a string!
            {
                return Convert.ToInt32(o) != 0;
            }

            string? s = o.ToString();    // cast to intermediate string.
            if (string.IsNullOrWhiteSpace(s))
                return defVal;

            // Be liberal/forgiving.
            if (defVal)
            {
                if (ValidState.IsFalse(s))
                    return false;
            }
            else
            {
                if (ValidState.IsTrue(s))
                    return true;
            }

            if (bool.TryParse(s, out bool valBool))     // The strict conversion.  Boolean.TrueString,  Boolean.FalseString 
                return valBool;

            if (int.TryParse(s, out int valInt))     // some sort of number? not 0.
                return valInt != 0;

            return defVal;
        }

        /// <summary>
        /// convert object? to a string. don't return null.
        /// should never throw. (safer than o.ToString())
        /// </summary>
        /// <param name="o"></param>
        /// <param name="nullStr">default value</param>
        /// <returns></returns>
        public static string ToString(object? o, string nullStr)
        {
            if (ValidState.IsNull(o))
                return nullStr;    // NEVER null. use ToStringN() for that.
            return o.ToString() ?? nullStr;
        }

        /// <summary>
        /// Convert object? to a safe (non null) string. 
        /// should never throw. (safer than o.ToString())
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string ToString(object? o)
        {
            if (ValidState.IsNull(o))
                return string.Empty;    // NEVER null. use ToStringN() for that.
            return o.ToString() ?? string.Empty;
        }

        /// <summary>
        /// convert object? to a nullable string. strings are always nullable.
        /// should never throw. (safer than o.ToString())
        /// </summary>
        /// <param name="o"></param>
        /// <param name="whitenull"></param>
        /// <returns></returns>
        public static string? ToStringN(object? o, bool whitenull = true)
        {
            if (ValidState.IsNull(o))
                return null;
            string? s = o.ToString();
            if (whitenull && string.IsNullOrWhiteSpace(s))
                return null;
            return s;
        }

        /// <summary>
        /// un-box object? to int. handle null and DBNull to defVal.
        /// should never throw.
        /// defVal = 0
        /// @note strings with extra junk at end fail. "01FA". The conversion fails because the string cannot contain hexadecimal digits; 
        /// </summary>
        /// <param name="o">object?</param>
        /// <param name="defVal">0</param>
        /// <returns></returns>
        public static int ToInt(object? o, int defVal = 0)
        {
            if (ValidState.IsNull(o))
                return defVal;

            Type type = o.GetType();
            if (type == typeof(int))     // faster convert
                return (int)o;
            if (type == typeof(int?))     // faster convert
                return ((int?)o) ?? defVal;
            if (type == typeof(System.Enum))     // dont convert Enum to a string!
            {
                // NOTE: enum can have a base type of long ???
                // return (int)Convert.ChangeType(o, typeof(int));
                return Convert.ToInt32(o);
            }

            string? s = o.ToString();    // cast to intermediate string.
            if (string.IsNullOrWhiteSpace(s))
                return defVal;

            if (!int.TryParse(s, out int val))
                return defVal;

            return val;
        }

        public static int ToIntSloppy(string? s, int offset = 0)
        {
            // try to read a string. End on any non digit. skip leading spaces.
            if (s == null)
                return 0;
            int val = 0;
            bool leadSpace = true;  // ignore lead spaces.
            for (int i = offset; i < s.Length; i++)
            {
                char ch = s[i];
                if (leadSpace && char.IsWhiteSpace(ch))
                    continue;
                if (!StringUtil.IsDigit1(ch))
                    break;
                leadSpace = false;  // found a non space.
                val = (val * 10) + (ch - '0');
            }
            return val;
        }

        public static ulong ToULong(object? o)
        {
            // Used for NPI. 10 digit integer. https://en.wikipedia.org/wiki/National_Provider_Identifier
            if (ValidState.IsNull(o))
                return 0;

            Type type = o.GetType();
            if (type == typeof(int))     // faster convert
                return (ulong)(int)o;
            if (type == typeof(long))     // faster convert
                return (ulong)(long)o;
            if (type == typeof(int?))     // faster convert
                return (ulong)(((int?)o) ?? 0);
            if (type == typeof(System.Enum))     // don't convert this to a string!
            {
                // return (long)Convert.ChangeType(o, typeof(long));
                return (ulong)Convert.ToInt32(o);
            }

            string? s = o.ToString();    // cast to intermediate string.
            if (string.IsNullOrWhiteSpace(s))
                return 0;
            if (!ulong.TryParse(s, out ulong val))
                return 0;

            return val;
        }

        public static DateTime ToDateTime(object? o)
        {
            // should not throw.
            // defVal = DateTime.MinValue
            // MySQL needs ;Convert Zero Datetime=True

            if (ValidState.IsNull(o))
                return DateTime.MinValue;

            Type type = o.GetType();
            if (type == typeof(DateTime))     // faster convert
                return (DateTime)o;
            if (type == typeof(DateTime?))     // faster convert
                return ((DateTime?)o) ?? DateTime.MinValue;

            string? s = o.ToString();    // cast to intermediate string.
            if (string.IsNullOrWhiteSpace(s))
                return DateTime.MinValue;

            if (!DateTime.TryParse(s, out DateTime val))
                return DateTime.MinValue;
            return val;
        }

        public static DateTime ToDateTime(DateTime? dt)
        {
            if (!dt.HasValue || dt.IsExtremeDate())
                return DateTime.MinValue;
            return dt.Value;
        }

        public static DateTime? ToDateTimeN(DateTime? dt)
        {
            // We use "" as null sometimes from the db. convert it back to null for storage in the db.
            // TODO the same as DBNull.Value ?

            if (!dt.HasValue || dt.IsExtremeDate())
                return null;
            return dt;
        }

        public static DateTime? ToDateTimeN(object? o)
        {
            DateTime dt = ToDateTime(o);
            if (dt.IsExtremeDate())
                return null;
            return dt;
        }

        public static double ToDouble(object? o, double defVal = 0)
        {
            if (ValidState.IsNull(o))
                return defVal;

            Type type = o.GetType();
            if (type == typeof(double))     // faster convert
                return (double)o;
            if (type == typeof(double?))     // faster convert
                return ((double?)o) ?? defVal;

            if (type == typeof(float))     // faster convert
                return (float)o;
            if (type == typeof(float?))     // faster convert
                return ((float?)o) ?? defVal;

            string? s = o.ToString();    // cast to intermediate string.
            if (string.IsNullOrWhiteSpace(s))
                return defVal;
            if (!double.TryParse(s, out double val))
                return defVal;

            return val;
        }

        public static decimal ToDecimal(object? o, decimal defVal = 0m)
        {
            // defVal = 0
            if (ValidState.IsNull(o))
                return defVal;

            Type type = o.GetType();
            if (type == typeof(decimal))     // faster convert
                return (decimal)o;
            if (type == typeof(decimal?))     // faster convert
                return ((decimal?)o) ?? defVal;
            if (type == typeof(System.Enum))     // dont convert this to a string!
            {
                return Convert.ToInt32(o);
            }

            string? s = o.ToString();    // cast to intermediate string.
            if (string.IsNullOrWhiteSpace(s))
                return defVal;
            if (!decimal.TryParse(s, out decimal val))
                return defVal;

            return val;
        }

        public static int? ToId(int? id)
        {
            // We use 0 as null sometimes from the db . convert it back to null for storage in the db.
            if (!ValidState.IsValidId(id))      // kInvalidId
                return null;
            return id;
        }
        public static int? ToId(Enum id)
        {
            // We use 0 as null sometimes from the db . convert it back to null for storage in the db.
            int idN = id.ToInt();
            if (!ValidState.IsValidId(idN))      // kInvalidId
                return null;
            return idN;
        }
        public static int FromId(int? id)
        {
            // may be cast to enum ?
            return id ?? ValidState.kInvalidId;
        }

        public static string? ToUnique(string? id)
        {
            // A string FK to some other table.
            if (!ValidState.IsValidUnique(id))
                return null;        // was not valid!
            return id;
        }

        /// <summary>
        /// Round some int to nearest multiple of roundTo
        /// </summary>
        /// <param name="value"></param>
        /// <param name="roundTo"></param>
        /// <returns></returns>
        public static int RoundTo(int value, int roundTo)
        {
            var remainder = value % roundTo;
            var r2 = roundTo - remainder;
            var result = remainder < r2
                ? (value - remainder) // round down
                : (value + r2); // round up
            return result;
        }
        public static long RoundTo(long value, long roundTo)
        {
            var remainder = value % roundTo;
            var r2 = roundTo - remainder;
            var result = remainder < r2
                ? (value - remainder) // round down
                : (value + r2); // round up
            return result;
        }

        public static bool IsNullableType(Type t)
        {
            // TypeCode.String is an odd case.
            // like (Nullable.GetUnderlyingType(t) != null) but works with real generics.
            if (t.IsGenericType)
            {
                if ( t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    return true;
            } 
            return false;
        }

        static object? GetSingle(IEnumerable a)
        {
            var a2 = a.Cast<object>();
            if (a2 == null || a2.Count() != 1)
                return null;
            return a2.Single();
        }

        /// <remarks>
        /// This method exists as a workaround to System.Convert.ChangeType(Object, Type) which does not handle
        /// nullables as of version 2.0 (2.0.50727.42) of the .NET Framework.
        /// Try to be much more forgiving than Convert.ChangeType for enums, bool and int.
        /// </remarks>
        public static object? ChangeType(object? value, Type convertToType)
        {
            // Note: This if block was taken from Convert.ChangeType as is, and is needed here since we're
            // checking properties on conversionType below.

            ValidState.ThrowIfNull(convertToType, nameof(convertToType));
            if (ValidState.IsNull(value))  // NULL or DBNull is always null.
                return null;

            // Array of a single value can act like the single value for my purposes.  
            Type convertFromType = value.GetType();
            TypeCode typeCodeFrom = Type.GetTypeCode(convertFromType);

            if (typeCodeFrom == TypeCode.Object)    // NOT string. for 'StringValues'
            {
                if (value is IEnumerable array)
                {
                    value = GetSingle(array);
                    if (value == null)  // Cant handle this.
                        return null;
                }
            }
            if (typeCodeFrom == TypeCode.String && convertToType == typeof(byte[]))
            {
                if (value.ToString() == "...")   // Weird MySQL export thing.
                    return null;
            }

            // If it's not a nullable type, just pass through the parameters to Convert.ChangeType
            if (IsNullableType(convertToType))
            {
                // It's a nullable type, so instead of calling Convert.ChangeType directly which would throw a
                // InvalidCastException (per http://weblogs.asp.net/pjohnson/archive/2006/02/07/437631.aspx),

                // It's a nullable type, and not null, so that means it can be converted to its underlying type,
                // so overwrite the passed-in conversion type with this underlying type
                var nullableConverter = new NullableConverter(convertToType);
                convertToType = nullableConverter.UnderlyingType;
            }

            // BEWARE ENUMS HERE . Invalid cast of int to enum.
            if (convertToType.BaseType == typeof(System.Enum))
            {
                return Enum.ToObject(convertToType, Converter.ToInt(value));
            }

            if (convertToType == typeof(string))
            {
                return value.ToString();        // anything can be a string.
            }
            if (convertToType == typeof(bool))   // Be more forgiving converting to bool. "1" = true.
            {
                return ToBool(value);
            }
            if (convertToType == typeof(int))
            {
                return ToInt(value);
            }
            if (convertToType == typeof(System.DateOnly) && typeCodeFrom == TypeCode.String)
            {
                // Not handled below !?
                return System.DateOnly.Parse(value.ToString() ?? string.Empty);
            }

            // Now that we've guaranteed conversionType is something Convert.ChangeType can handle (i.e. not a
            // nullable type), pass the call on to Convert.ChangeType
            return Convert.ChangeType(value, convertToType, CultureInfo.InvariantCulture);
        }

        public static MemoryStream ToMemoryStream(string str)
        {
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(str);
            writer.Flush();
            ms.Position = 0;
            return ms;
        }

        public static System.Collections.Generic.List<ItemType> ToListDupe<ItemType>(System.Collections.Generic.IEnumerable<ItemType> a)
        {
            // CopyTo object array into new List. IEnumerable. DO NOT clone all objects.
            // Dupe this list. Binding to DataSource in ListBoxes/ComboBoxes seems to like this. (if multi boxes use same source)
            var oList = new System.Collections.Generic.List<ItemType>();
            foreach (ItemType i in a)
            {
                oList.Add(i);
            }
            return oList;
        }

        public static int[] ToArray(params int[] array)
        {
            // Simple convert from variable args to array.
            return array;
        }
    }
}
