using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace DotStd
{
    public interface IPropertyGetter
    {
        // Get a property that may be stored in many different ways.
        // e.g. Properties can be stored via object reflection, Dictionary, DataTable
        // children names can be expressed via "parent:child" syntax

        object GetPropertyValue(string name);
    }

    public interface IPropertySetter
    {
        // like IDynamicMetaObjectProvider, DynamicObject, ExpandoObject, etc.

        void SetPropertyValue(string name, object val);
    }

    public abstract class PropertyBagBase : IPropertyGetter
    {
        public abstract object GetPropertyValue(string name);

        public string ReplaceTokenX(string s, string errorStr = null)
        {
            return Formatter.ReplaceTokenX(s, this, errorStr);
        }
    }

    public class PropertyBagObj : PropertyBagBase, IPropertySetter
    {
        // Just use reflection to get a properties value from an object. IPropertyGetter
        // Assume i CANNOT just add new props on the fly.

        object _Obj; // just use Type reflection on this.
        public Type Type { get; set; }  // Maybe not the same as Obj.GetType()
        public bool IsCaseSensative { get; set; } = true;

        public PropertyBagObj()
        {
        }
        public PropertyBagObj(object obj, Type typeSrc = null, bool isCaseSensative = true)
        {
            _Obj = obj;
            Type = typeSrc;
            IsCaseSensative = isCaseSensative;
        }

        public override object GetPropertyValue(string name)
        {
            if (_Obj == null)
                return null;

            if (Type == null)
            {
                Type = _Obj.GetType();   // use default type.
            }

            PropertyInfo prop = IsCaseSensative ? Type.GetProperty(name)
                : Type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);  
            if (prop == null || !prop.CanRead)
                return null;

            return prop.GetValue(_Obj, null);
        }

        public virtual void SetPropertyValue(string name, object val)
        {
            // MUST set Obj first!
            // Throw if Obj is null or name prop does not exist

            if (Type == null)
            {
                Type = _Obj.GetType();
            }

            PropertyInfo prop = Type.GetProperty(name);
            //if (prop == null || ! prop.CanWrite)   // throw ??
            //  return;

            prop.SetValue(_Obj, val);
        }
    }

    public class PropertyBagDic : PropertyBagBase, IPropertySetter
    {
        // a IPropertyGetter implemented as a Dictionary
        // Assume i CAN add new props on the fly.

        Dictionary<string, object> _Props;

        public PropertyBagDic()
        {
        }
        public PropertyBagDic(Dictionary<string, object> props)
        {
            this._Props = props;
        }

        private void CheckProps()
        {
            // if the dictionary doesn't yet exist then create it.
            if (_Props == null)
            {
                _Props = new Dictionary<string, object>();
            }
        }

        public void AddProperties(object obj)
        {
            // add all CanRead properties from object to the bag.
            Type fromType = obj.GetType();
            CheckProps();
            foreach (PropertyInfo propFrom in fromType.GetProperties())
            {
                if (!propFrom.CanRead)
                    continue;
                object val = propFrom.GetValue(obj, null);
                _Props[propFrom.Name] = val;
            }
        }

        public override object GetPropertyValue(string name) // IPropertyGetter
        {
            if (_Props == null)
                return null;

            object val;
            if (!_Props.TryGetValue(name, out val))
                return null;

            return val;
        }

        public virtual void SetPropertyValue(string name, object val) // IPropertySetter
        {
            CheckProps();
            _Props[name] = val; // overwrite if existing.
        }
    }

    public class PropertyBagKeyValue : PropertyBagBase
    {
        // use with this.Request.Form, IFormCollection

        readonly IEnumerable<KeyValuePair<string, StringValues>> Row;

        public PropertyBagKeyValue(IEnumerable<KeyValuePair<string, StringValues>> row)
        {
            Row = row;
        }
        public override object GetPropertyValue(string name)
        {
            foreach (var x in Row)
            {
                if (x.Key.CompareNoCase(name) == 0)
                    return x.Value;
            }
            return null;
        }
    }

    public class PropertyBagRow : PropertyBagBase
    {
        // a IPropertyGetter implemented as a DataRow

        DataRow Row;

        public PropertyBagRow()
        {
        }

        public PropertyBagRow(DataRow row)
        {
            Row = row;
        }

        public override object GetPropertyValue(string name)
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

    public class PropertyUtil
    {
        // util class for abstracting a property bag.

        public static object GetPropertyValue(object fromObj, string propertyName)
        {
            // Get 1 single prop value via reflection.
            // @return null if i cant get a value.

            if (fromObj == null)
                return null;
            Type fromType = fromObj.GetType();
            PropertyInfo prop = fromType.GetProperty(propertyName);
            if (!prop.CanRead)
                return null;
            return prop.GetValue(fromObj, null);
        }

        public static void SetPropertyValue(object toObj, string name, object value)
        {
            // Set 1 single prop value via reflection.
            if (toObj == null)
                return;
            Type toType = toObj.GetType();
            PropertyInfo prop = toType.GetProperty(name);
            // prop == null || ! prop.CanWrite
            prop.SetValue(toObj, value);
        }

        public static int InjectProperties<T>(T toObj, object fromObj, List<string> ignored = null)
        {
            // Inject all properties that match. (not fields or events) like IPropertySetter
            // fromObj is some type derived from T. may have many more props but we will ignore them. Only use T Props
            // NOTE: We intentionally DON'T use toObj.GetType() here because we want explicit caller control of the type. (could just be a child type)
            // RETURN: propsCopied . Caller should throw if this is not the correct number !

            if (fromObj == null)
                return 0;

            Type ignoreAttrType = typeof(IgnoreDataMemberAttribute);    // CHECK [IgnoreDataMember]

            int propsCopied = 0;
            Type toType = typeof(T);
            Type fromType = fromObj.GetType();
            bool sameType = toType.IsAssignableFrom(fromType);

            foreach (PropertyInfo propTo in toType.GetProperties())
            {
                if (!propTo.CanWrite)
                    continue;

                // Find eqiv prop by name.
                PropertyInfo propFrom = sameType ? propTo : fromType.GetProperty(propTo.Name);
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

                object val = propFrom.GetValue(fromObj, null);
                propTo.SetValue(toObj, val, null);      // like IPropertySetter
                propsCopied++;
            }
            return propsCopied;
        }

        public static int InjectProperties<T>(T toObj, IPropertyGetter from, string from_prefix = null)
        {
            // inject all the properties from 'from' into an object toObj that match.
            // Just skip those that don't match
            // NOTE: We intentionally DON'T use toObj.GetType() here because we want explicit caller control of the type. (could just be a child type)

            if (from == null)
                return 0;
            int propsCopied = 0;
            Type toType = typeof(T);
            foreach (PropertyInfo propTo in toType.GetProperties())
            {
                if (!propTo.CanWrite)
                    continue;
                object valFrom = from.GetPropertyValue(string.Concat(from_prefix, propTo.Name));    // has a matching prop?
                if (valFrom == null)
                    continue;

                object valTo = Converter.ChangeType(valFrom, propTo.PropertyType);

                propTo.SetValue(toObj, valTo, null); // ChangeType probably not needed?
                propsCopied++;
            }
            return propsCopied;
        }

        public static int InjectProperties<T>(T toObj, IEnumerable<KeyValuePair<string, StringValues>> src)
        {
            return InjectProperties(toObj, new PropertyBagKeyValue(src));
        }

        public static T CreateCloneT<T>(object fromObj)
        {
            // Create a clone of some object as a target type. Maybe a child type of fromObj.
            // Use reflection to clone props.
            if (fromObj == null)
                return default(T);
            T toObj = (T)Activator.CreateInstance(typeof(T));
            InjectProperties<T>(toObj, fromObj);
            return toObj;
        }

        public static T CreateCloneSerial<T>(T source)
        {
            // like CreateCloneT but uses serialization.
            // serializer can offer more depth and honors serialization attributes.

            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}
