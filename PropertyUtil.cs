using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace DotStd
{
    /// <summary>
    /// Get a property that may be stored in many different ways.
    /// e.g. Properties can be stored via object reflection, Dictionary, DataTable
    /// children names can be expressed via "parent:child" syntax
    /// Similar to C# dynamic objects.
    /// </summary>
    public interface IPropertyGetter
    {
        object? GetPropertyValue(string name);
    }

    public interface IPropertySetter
    {
        // like IDynamicMetaObjectProvider, DynamicObject, ExpandoObject, etc.
        void SetPropertyValue(string name, object val);
    }

    public abstract class PropertyBagBase : IPropertyGetter
    {
        public abstract object? GetPropertyValue(string name);

        public string ReplaceTokenX(string s, string? errorStr = null)
        {
            return Formatter.ReplaceTokenX(s, this, errorStr);
        }
    }

    /// <summary>
    /// use reflection to get a properties value from an object. IPropertyGetter
    /// Assume i CANNOT just add new props on the fly.
    /// </summary>
    public class PropertyBagObj : PropertyBagBase, IPropertySetter
    {
        public readonly object? Obj; // just use Type reflection on this.
        public Type? Type { get; set; }  // Type for Obj. Maybe not the same as Obj.GetType(). // type may be resolved later.
        public bool IsCaseSensitive { get; set; } = true;

        public PropertyBagObj()
        {
        }
        public PropertyBagObj(object? obj, Type? typeSrc = null, bool isCaseSensitive = true)
        {
            Obj = obj;
            Type = typeSrc; // type may be resolved later.
            IsCaseSensitive = isCaseSensitive;
        }

        /// <summary>
        /// Get object property by name.
        /// Allow complex names.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override object? GetPropertyValue(string name)
        {
            if (Obj == null)
                return null;

            if (Type == null)
            {
                Type = Obj.GetType();   // use default type.
            }

            // Parse out a sub property after the . (or [ ?)
            string? nameChild = null;
            int i = name.IndexOf('.');
            if (i >= 0)
            {
                nameChild = name.Substring(i + 1);
                name = name.Substring(0, i);
            }

            PropertyInfo? prop = IsCaseSensitive ? Type.GetProperty(name)
                : Type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.CanRead)
                return null;

            object? obj = prop.GetValue(Obj, null);
            if (nameChild != null && obj != null)
            {
                // Assume obj has properties of its own.
                return (new PropertyBagObj(obj)).GetPropertyValue(nameChild);
            }

            return obj;
        }

        /// <summary>
        /// Set Obj Property
        /// Throw if Obj is null or name prop does not exist
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public virtual void SetPropertyValue(string name, object val)
        {
            ValidState.ThrowIfNull(Obj, nameof(Obj));
            if (Type == null)
            {
                Type = Obj.GetType();
                ValidState.ThrowIfNull(Type, nameof(Type));
            }

            PropertyInfo? prop = Type.GetProperty(name);
            ValidState.ThrowIfNull(prop, nameof(prop));

            // !prop.CanWrite // test ?
            prop.SetValue(Obj, val);
        }
    }

    /// <summary>
    /// a IPropertyGetter implemented as a Dictionary
    /// Assume i CAN add new props on the fly.
    /// </summary>
    public class PropertyBagDic : PropertyBagBase, IPropertySetter
    {
        readonly Dictionary<string, object> _Props;

        public static bool HasProp([NotNullWhen(true)] dynamic obj, string name)
        {
            // Test ExpandoObject
            if (obj == null) return false;
            if (obj is IDictionary<string, object> dict1)
                return dict1.ContainsKey(name);
            return obj.GetType().GetProperty(name) != null;
        }

        public PropertyBagDic()
        {
            this._Props = new Dictionary<string, object>();
        }
        public PropertyBagDic(Dictionary<string, object> props)
        {
            this._Props = props;
        }

        public override object? GetPropertyValue(string name) // IPropertyGetter
        {
            if (!_Props.TryGetValue(name, out object? val))
                return null;
            return val;
        }

        public virtual void SetPropertyValue(string name, object val) // IPropertySetter
        {
            _Props[name] = val; // overwrite if existing.
        }

        /// <summary>
        /// add all CanRead properties from object to the bag.
        /// </summary>
        /// <param name="obj"></param>
        public void AddProperties(object obj)
        {
            Type fromType = obj.GetType();
            foreach (PropertyInfo propFrom in fromType.GetProperties())
            {
                if (!propFrom.CanRead)
                    continue;
                object? val = propFrom.GetValue(obj, null);
                if (val == null)
                    continue;
                SetPropertyValue(propFrom.Name, val);
            }
        }
    }

    /// <summary>
    /// use with this.Request.Form, IFormCollection
    /// </summary>
    public class PropertyBagKeyValue : PropertyBagBase
    {
        readonly IEnumerable<KeyValuePair<string, StringValues>> Row;

        public PropertyBagKeyValue(IEnumerable<KeyValuePair<string, StringValues>> row)
        {
            Row = row;
        }
        public override object? GetPropertyValue(string name)
        {
            foreach (var x in Row)
            {
                if (x.Key.CompareNoCase(name) == 0)
                    return x.Value;
            }
            return null;
        }
    }

    /// <summary>
    /// a IPropertyGetter implemented as a DataRow
    /// </summary>
    public class PropertyBagRow : PropertyBagBase
    {
        public DataRow? Row;

        public PropertyBagRow()
        {
        }

        public PropertyBagRow(DataRow row)
        {
            Row = row;
        }

        public override object? GetPropertyValue(string name)
        {
            // Row.Table = provide field names for Row

            if (Row == null)
                return null;
            if (Row.Table == null || Row.Table.Columns.Count <= 0)
                return null;
            if (!Row.Table.Columns.Contains(name))    // does table have this ?
                return null;
            return Row[name];
        }
    }

    /// <summary>
    /// util class for abstracting a property bag.
    /// </summary>
    public class PropertyUtil
    {
        public static object? GetPropertyValue(object fromObj, string propertyName)
        {
            // Get 1 single prop value via reflection.
            // @return null if i cant get a value.

            if (fromObj == null)
                return null;
            Type fromType = fromObj.GetType();
            PropertyInfo? prop = fromType.GetProperty(propertyName);
            if (prop == null || !prop.CanRead)
                return null;
            return prop.GetValue(fromObj, null);
        }

        /// <summary>
        /// Set 1 single prop value via reflection.
        /// </summary>
        /// <param name="toObj"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void SetPropertyValue(object toObj, string name, object value)
        {
            if (toObj == null)
                return;
            Type toType = toObj.GetType();
            PropertyInfo? prop = toType.GetProperty(name);
            if (prop == null)
                return;
            // ! prop.CanWrite
            prop.SetValue(toObj, value);
        }

        /// <summary>
        /// Inject all properties that match. (not fields or events) like IPropertySetter
        /// NOTE: We intentionally DON'T use toObj.GetType() here because we want explicit caller control of the type. (could just be a child type)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toObj"></param>
        /// <param name="fromObj">is some type derived from T. may have many more props but we will ignore them. Only use T Props</param>
        /// <param name="ignored"></param>
        /// <returns>propsCopied . Caller should throw if this is not the correct number !</returns>
        public static int InjectProperties<T>(T toObj, object? fromObj, List<string>? ignored = null)
        {
            if (fromObj == null)
                return 0;

            Type ignoreAttrType = typeof(IgnoreDataMemberAttribute);    // CHECK [IgnoreDataMember] or [Ignore()] ?

            int propsCopied = 0;
            Type toType = typeof(T);
            Type fromType = fromObj.GetType();
            bool sameType = toType.IsAssignableFrom(fromType);

            foreach (PropertyInfo propTo in toType.GetProperties())
            {
                if (!propTo.CanWrite)
                    continue;

                // Find equiv prop by name.
                PropertyInfo? propFrom = sameType ? propTo : fromType.GetProperty(propTo.Name);
                if (propFrom == null || !propFrom.CanRead)
                    continue;

                // Stuff to ignore?
                object[] attrs2 = propFrom.GetCustomAttributes(ignoreAttrType, false);  // This was probably not populated correctly so ignore it.
                if (attrs2.Length > 0)
                    continue;
                if (ignored != null && ignored.Contains(propTo.Name))
                {
                    continue;   // skip this.
                }

                object? val = propFrom.GetValue(fromObj, null);
                propTo.SetValue(toObj, val, null);      // like IPropertySetter
                propsCopied++;
            }
            return propsCopied;
        }

        /// <summary>
        /// inject all the properties from 'from' into an object toObj that match.
        /// NOTE: This does not know how to handle child objects with props. e.g. Dtr[Start]
        /// Just skip those that don't match
        /// NOTE: We intentionally DON'T use toObj.GetType() here because we want explicit caller control of the type. (could just be a child type)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toObj"></param>
        /// <param name="from"></param>
        /// <param name="from_prefix"></param>
        /// <returns></returns>
        public static int InjectProperties<T>(T toObj, IPropertyGetter? from, string? from_prefix = null)
        {
            if (from == null)
                return 0;
            int propsCopied = 0;
            Type toType = typeof(T);
            foreach (PropertyInfo propTo in toType.GetProperties())
            {
                if (!propTo.CanWrite)
                    continue;
                object? valFrom = from.GetPropertyValue(string.Concat(from_prefix, propTo.Name));    // has a matching prop?
                if (valFrom == null)
                    continue;

                object? valTo = Converter.ChangeType(valFrom, propTo.PropertyType);

                propTo.SetValue(toObj, valTo, null); // ChangeType probably not needed?
                propsCopied++;
            }
            return propsCopied;
        }

        public static int InjectProperties<T>(T toObj, IEnumerable<KeyValuePair<string, StringValues>> src)
        {
            return InjectProperties(toObj, new PropertyBagKeyValue(src));
        }

        /// <summary>
        /// Create a clone of some object as a target type. Maybe a child type of fromObj.
        /// Use reflection to clone props.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fromObj"></param>
        /// <returns></returns>
        public static T CreateCloneT<T>(object fromObj)
        {
            T? toObj = (T?)Activator.CreateInstance(typeof(T));
            ValidState.ThrowIfNull(toObj, nameof(T));   // should never happen!
            InjectProperties<T>(toObj, fromObj);
            return toObj;
        }
    }
}
