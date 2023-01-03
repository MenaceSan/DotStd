using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace DotStd
{
    /// <summary>
    /// Direction of sort. like System.Web.UI.WebControls.SortDirectionNullable
    /// </summary>
    public enum SortDirection
    {
        None = 0,
        Ascending,  // default.
        Descending,
    }

    /// <summary>
    /// How should a field be sorted ? Comparer.cs
    /// This can be related to LambdaExpression? GetPropertyExp(string name)
    /// </summary>
    public class ComparerDef
    {
        public string PropName { get; set; }        // Reflection property name of some object.

        public SortDirection SortDir { get; set; }

        public ComparerDef(string PropName, SortDirection SortDir = SortDirection.Ascending)
        {
            this.PropName = PropName;
            this.SortDir = SortDir;
        }

        /// <summary>
        /// Compare 2 simple typed objects. 
        /// Assume both objects are the same type! TypeCode
        /// Allow nulls
        /// String is compared case ignored.
        /// </summary>
        /// <param name="ox"></param>
        /// <param name="oy">oy is not typically null ?</param>
        /// <param name="typeCode"></param>
        /// <returns>0=equal, >0=x is greater (1), <0=x is lesser. (-1)</returns>
        public static int CompareType(object? ox, object? oy, TypeCode typeCode)
        {
            if (ox == null || oy == null)
            {
                if (ox == oy)
                    return 0;
                if (ox == null)
                    return -1;
                return 1;
            }
            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.String:
                    return String.Compare(Convert.ToString(ox), Convert.ToString(oy), true);
                case TypeCode.DateTime:
                    return DateTime.Compare(Convert.ToDateTime(ox), Convert.ToDateTime(oy));
                case TypeCode.Decimal:   // "System.Decimal":  // Money
                    return Decimal.Compare(Convert.ToDecimal(ox), Convert.ToDecimal(oy));
                case TypeCode.Int64:
                    {
                        Int64 ix = Convert.ToInt64(ox);
                        Int64 iy = Convert.ToInt64(oy);
                        if (ix > iy)
                            return 1;
                        else if (ix < iy)
                            return -1;
                    }
                    return 0;
                case TypeCode.Double:
                    {
                        Double ix = Convert.ToDouble(ox);
                        Double iy = Convert.ToDouble(oy);
                        if (ix > iy)
                            return 1;
                        else if (ix < iy)
                            return -1;
                    }
                    return 0;
                default:
                    // Numeric's are treated differently.
                    return ox.GetHashCode() - oy.GetHashCode();
            }
        }

        public static int CompareType(object? ox, object? oy, TypeCode eTypeCodeX, TypeCode eTypeCodeY)
        {
            // we dont know if they are the same type.
            if (eTypeCodeX == eTypeCodeY || ox == null || oy == null)
            {
                return CompareType(ox, oy, eTypeCodeX);
            }
            // diff types! compare as strings.
            return CompareType(ox.ToString(), oy.ToString(), TypeCode.String);
        }
    }

    public class ComparerSimple : ComparerDef, System.Collections.IComparer
    {
        // Compare based on a reflected property (string field name) of some object.

        protected System.Reflection.PropertyInfo? _oProp;   // cache this.

        public ComparerSimple(string _PropName, SortDirection _SortDir = SortDirection.Ascending)
            : base(_PropName, _SortDir)
        {
        }

        public virtual int Compare(object? x, object? y)
        {
            // Compare properties of 2 objects.
            // like System.Collections.Generic.Comparer<object>.Default.Compare(col1, col2);
            // RETURN: 0=equal, >0=x is greater (1), <0=x is lesser. (-1)
            // ASSUME objects are the same type.

            if (this.SortDir == SortDirection.None)
            {
                return 0;
            }
            if (_oProp == null)
            {
                if (x != null)
                {
                    _oProp = x.GetType().GetProperty(this.PropName);
                }
                else if (y != null)
                {
                    _oProp = y.GetType().GetProperty(this.PropName);
                }
                else
                {
                    return 0;
                }
                ValidState.ThrowIfNull(_oProp, nameof(_oProp));
            }
            int iRet = CompareType(_oProp.GetValue(x, null), _oProp.GetValue(y, null), Type.GetTypeCode(_oProp.PropertyType));
            // Reverse?
            if (this.SortDir == SortDirection.Descending)
                iRet *= -1;
            return iRet;
        }
    }

    public class ComparerGeneric<ItemType> : ComparerSimple, System.Collections.Generic.IComparer<ItemType>
    {
        // compare using a generic <ItemType> .
        // ItemType = a complex structure with a PropName value to be sorted.

        public ComparerGeneric(string PropName, SortDirection SortDir = SortDirection.Ascending)
          : base(PropName, SortDir)
        {
        }

        public virtual int Compare(ItemType? x, ItemType? y)
        {
            // Compare properties of 2 objects.
            if (this.SortDir == SortDirection.None)
            {
                return 0;
            }
            if (_oProp == null) // cache this.
            {
                _oProp = typeof(ItemType).GetProperty(this.PropName);
                ValidState.ThrowIfNull(_oProp, nameof(_oProp));
            }
            int iRet = CompareType(_oProp.GetValue(x, null), _oProp.GetValue(y, null), Type.GetTypeCode(_oProp.PropertyType));
            // Reverse?
            if (this.SortDir == SortDirection.Descending)
                iRet *= -1;
            return iRet;
        }
    }

    /// <summary>
    /// General helpers for compare.
    /// </summary>
    public static class ComparerUtil
    {
        public static IEnumerable<T> GetSortedList<T>(IEnumerable<T> src, string sortColumn, SortDirection sortDir)
        {
            // Sort this list in memory. 

            PropertyInfo? prop = typeof(T).GetProperty(sortColumn);
            if (prop == null)
                return src;
            int multiplier = sortDir == SortDirection.Descending ? -1 : 1;

            var list = new List<T>();
            list.AddRange(src);
            list.Sort((t1, t2) =>
            {
                var col1 = prop.GetValue(t1);
                var col2 = prop.GetValue(t2);
                return multiplier * Comparer<object>.Default.Compare(col1, col2);
            });
            return list;
        }

        public static Dictionary<string, object?>? DiffProps<T>(T? objNew, T? objOld)
        {
            // What props changed (is New) in these objects? 
            // NOTE: Does not compare fields JUST props.
            // null values are ok.
            // return dictionary of changes. null = nothing changed.

            Dictionary<string, object?>? changes = null;
            Type typeComp = typeof(T);
            Type ignoreAttrType = typeof(IgnoreDataMemberAttribute);    // CHECK [IgnoreDataMember] or [Ignore()] ?

            foreach (PropertyInfo prop in typeComp.GetProperties()) // enum props via reflection.
            {
                // Stuff to ignore?
                object[] attrs2 = prop.GetCustomAttributes(ignoreAttrType, false);  // This was probably not populated correctly so ignore it.
                if (attrs2.Length > 0)
                    continue;

                object? valNew = (objNew == null) ? null : prop.GetValue(objNew, null);
                object? valOld = (objOld == null) ? null : prop.GetValue(objOld, null);
                if (!object.Equals(valNew, valOld))
                {
                    if (changes == null)
                        changes = new Dictionary<string, object?>();
                    changes.Add(prop.Name, valNew);
                }
            }

            return changes;
        }
    }
}
