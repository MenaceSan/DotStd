using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace DotStd
{
    public enum SortDirection
    {
        // like System.Web.UI.WebControls.SortDirection
        None = 0,
        Ascending,  // default.
        Descending,
    }

    public class ComparerDef
    {
        // Comparer.cs
        // How should a field be sorted ?

        public string PropName { get; set; }        // Reflection property name of some object.

        public SortDirection SortDir { get; set; }

#if true
        public static int CompareBytes(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return a1.Length - a2.Length;

            for (int i = 0; i < a1.Length; i++)
                if (a1[i] != a2[i])
                    return a1[i] - a2[i];

            return 0;
        }
#else
        public static unsafe int CompareBytes(byte[] a1, byte[] a2)
        {
            // ASSUME a1 != a2 and both are not null.
            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1;
                byte* x2 = p2;
                int l = Math.Min(a1.Length, a2.Length);

                if (l >= 8)
                {
                    int l8 = l / 8;
                    for (int i = 0; i < l8; i++, x1 += 8, x2 += 8)
                        if (*((long*)x1) != *((long*)x2))
                            return (int)(x1 - p1);
                }

                // remainder.
                if ((l & 4) != 0)
                {
                    if (*((int*)x1) != *((int*)x2))
                        return (int)(x1 - p1);
                    x1 += 4; x2 += 4;
                }
                if ((l & 2) != 0)
                {
                    if (*((short*)x1) != *((short*)x2))
                        return (int)(x1 - p1);
                    x1 += 2; x2 += 2;
                }
                if ((l & 1) != 0)
                {
                    if (*((byte*)x1) != *((byte*)x2))
                        return (int)(x1 - p1);
                }
                return l;   // full match to min length of both.
            }
        }
#endif

        public static int CompareType(object ox, object oy, TypeCode eTypeCode)
        {
            // Compare 2 simple typed objects. 
            // Assume objects are the same type! TypeCode
            // Allow nulls
            // RETURN: 0=equal, >0=x is greater (1), <0=x is lesser. (-1)
            // String is compared case ignored.
            // oy is not typically null ?

            if (ox == null || oy == null)
            {
                if (ox == oy)
                    return 0;
                if (ox == null)
                    return -1;
                return 1;
            }
            switch (eTypeCode)
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

        public static int CompareType(object ox, object oy, TypeCode eTypeCodeX, TypeCode eTypeCodeY)
        {
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

        protected System.Reflection.PropertyInfo _oProp;

        public ComparerSimple(string _PropName, SortDirection _SortDir = SortDirection.Ascending)
        {
            PropName = _PropName;
            SortDir = _SortDir;
        }

        public virtual int Compare(object x, object y)
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
        // compare using a generic .
        // ItemType = a complex structure with a m_PropName value to be sorted.

        public ComparerGeneric(string PropName, SortDirection SortDir = SortDirection.Ascending)
          : base(PropName, SortDir)
        {
        }

        public virtual int Compare(ItemType x, ItemType y)
        {
            // Compare properties of 2 objects.
            if (this.SortDir == SortDirection.None)
            {
                return 0;
            }
            if (_oProp == null)
            {
                _oProp = typeof(ItemType).GetProperty(this.PropName);
            }
            int iRet = CompareType(_oProp.GetValue(x, null), _oProp.GetValue(y, null), Type.GetTypeCode(_oProp.PropertyType));
            // Reverse?
            if (this.SortDir == SortDirection.Descending)
                iRet *= -1;
            return iRet;
        }
    }

    public static class ComparerUtil
    {
        // General helpers for compare.

        public static IEnumerable<DataRow> GetSortedList(IEnumerable<DataRow> DataSource, string sortColumn, SortDirection sortDir)
        {
            // Numeric or string sort based on sortColumn type
            // could use DataTableExtensions.Field<dynamic> ??

            if (DataSource != null && DataSource.Any())
            {
#if NET40 // Framework
                if (sortDir == SortDirection.Ascending)
                    DataSource = DataSource.OrderBy(x => x.Field<dynamic>(sortColumn));
                else
                    DataSource = DataSource.OrderByDescending(x => x.Field<dynamic>(sortColumn));

#else   // NETCOREAPP
                // TODO Fix ME. use ComparerSimple.CompareType()
                DataRow row0 = DataSource.First();   // Get the type from the first.

#endif
            }
            return DataSource;
        }

        public static IEnumerable<T> GetSortedList<T>(IEnumerable<T> src, string sortColumn, SortDirection sortDir)
        {
            // Sort this list in memory. 

            var property = typeof(T).GetProperty(sortColumn);
            if (property == null)
                return src;
            int multiplier = sortDir == SortDirection.Descending ? -1 : 1;

            var list = new List<T>();
            list.AddRange(src);
            list.Sort((t1, t2) =>
            {
                var col1 = property.GetValue(t1);
                var col2 = property.GetValue(t2);
                return multiplier * Comparer<object>.Default.Compare(col1, col2);
            });
            return list;
        }

        public static Dictionary<string, object> DiffProps<T>(T objNew, T objOld)
        {
            // What props changed (is New) in these objects? 
            // NOTE: Does not compare fields JUST props.
            // null values are ok.
            // return dictionary of changes. null = nothing changed.

            Dictionary<string, object> changes = null;
            Type typeComp = typeof(T);
            foreach (PropertyInfo prop in typeComp.GetProperties()) // enum props via reflection.
            {
                object valNew = (objNew == null) ? null : prop.GetValue(objNew, null);
                object valOld = (objOld == null) ? null : prop.GetValue(objOld, null);
                if (!object.Equals(valNew, valOld))
                {
                    if (changes == null)
                        changes = new Dictionary<string, object>();
                    changes.Add(prop.Name, valNew);
                }
            }

            return changes;
        }
    }
}
