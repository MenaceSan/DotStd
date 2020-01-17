using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DotStd
{
    public static class DataPage
    {
        // Helper for paging of lists of data for Linq and EF. SQL.
        // Allow deferred (server side) paging.

        private static IOrderedQueryable<T> OrderingHelper5X<T, TK>(IQueryable<T> source, MemberExpression property, ParameterExpression param1, bool isSortAscending, bool thenByLevel)
        {
            // similar to OrderingHelper5 but the type must be known at compile time. But since there are few types we 
            var orderByExp = Expression.Lambda<Func<T, TK>>(property, param1);
            if (thenByLevel)
            {
                var source2 = (IOrderedQueryable<T>)source;
                if (isSortAscending)
                    return source2.ThenBy(orderByExp);
                else
                    return source2.ThenByDescending(orderByExp);
            }
            if (isSortAscending)
                return source.OrderBy(orderByExp);
            else
                return source.OrderByDescending(orderByExp);
        }

        private static IOrderedQueryable<T> OrderingHelper5<T>(IQueryable<T> source, string propertyName, bool isSortAscending, bool thenByLevel)
        {
            // UNUSED

            ParameterExpression param = Expression.Parameter(typeof(T), string.Empty); // I don't care about some naming?
            MemberExpression property = Expression.PropertyOrField(param, propertyName);    // T for prop. propertyName case not sensitive.

            TypeCode typeCodeProp = Type.GetTypeCode(property.Type);
            switch (typeCodeProp)
            {
                case TypeCode.String:
                    return OrderingHelper5X<T, string>(source, property, param, isSortAscending, thenByLevel);

                case TypeCode.Int32:
                    if (property.Type.IsEnum)
                    {
                        return OrderingHelper5X<T, Enum>(source, property, param, isSortAscending, thenByLevel);
                    }
                    else
                    {
                        return OrderingHelper5X<T, int>(source, property, param, isSortAscending, thenByLevel);
                    }
            }

            return null;
        }

        public static IOrderedQueryable<TEntityType> OrderingHelper3_TEST<TEntityType>(this IQueryable<TEntityType> query, string propertyname)
        {
            var param = Expression.Parameter(typeof(TEntityType), "s");
            var prop = Expression.PropertyOrField(param, propertyname);
            var sortLambda = Expression.Lambda(prop, param);

            Expression<Func<IOrderedQueryable<TEntityType>>> sortMethod = (() => query.OrderBy<TEntityType, object>(k => null));

            var methodCallExpression = (sortMethod.Body as MethodCallExpression);
            if (methodCallExpression == null)
                throw new Exception("Oops");

            var method = methodCallExpression.Method.GetGenericMethodDefinition();
            var genericSortMethod = method.MakeGenericMethod(typeof(TEntityType), prop.Type);
            var orderedQuery = (IOrderedQueryable<TEntityType>)genericSortMethod.Invoke(query, new object[] { query, sortLambda });

            return orderedQuery;
        }

        public static IOrderedQueryable<T> OrderingHelper4_TEST<T>(this IQueryable<T> source, string propertyName)
        {
            // Test this.
            var entityType = typeof(T);

            //Create x=>x.PropName
            var propertyInfo = entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy); // JavaScript can kill case.
            ParameterExpression param1 = Expression.Parameter(entityType, "x");
            MemberExpression property = Expression.Property(param1, propertyName);
            var orderByExp = Expression.Lambda(property, param1);

            //Get System.Linq.Queryable.OrderBy() method.
            var enumarableType = typeof(System.Linq.Queryable);
            var method = enumarableType.GetMethods()
                 .Where(m => m.Name == "OrderBy" && m.IsGenericMethodDefinition)
                 .Where(m =>
                 {
                     var parameters = m.GetParameters().ToList();
                     //Put more restriction here to ensure selecting the right overload                
                     return parameters.Count == 2;//overload that has 2 parameters
                 }).Single();

            //The LINQ's OrderBy<TSource, TKey> has two generic types, which provided here
            MethodInfo genericMethod = method.MakeGenericMethod(entityType, propertyInfo.PropertyType);

            /*Call query.OrderBy(selector), with query and selector: x=> x.PropName
              Note that we pass the selector as Expression to the method and we don't compile it.
              By doing so EF can extract "order by" columns and generate SQL for it.*/
            var newQuery = (IOrderedQueryable<T>)genericMethod
                 .Invoke(genericMethod, new object[] { source, orderByExp });
            return newQuery;
        }

        //***********************************//

        public static IOrderedQueryable<T> OrderingHelperX<T>(this IQueryable<T> source, Type typeProp, Expression orderByExp, bool isSortAscending = false, bool thenByLevel = false)
        {
            // Order by some named property.
            // This does work for EF 3.1 for MySQL with properly formed Expression.
            // e.g. Expression<Func<AgencySystemBiz, int>> orderByExp = (x => x.IdInt); q.OrderingHelper1X(typeof(int), orderByExp);

            MethodCallExpression call = Expression.Call(
                 typeof(Queryable),
                 thenByLevel ? (isSortAscending ? nameof(Queryable.ThenBy) : nameof(Queryable.ThenByDescending)) : (isSortAscending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending)),
                 new Type[] { typeof(T), typeProp },
                 source.Expression,
                 Expression.Quote(orderByExp));

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
        }

        //***********************************//

        private static IOrderedQueryable<T> OrderingHelper1<T>(IQueryable<T> source, string propertyName, bool isSortAscending, bool thenByLevel)
        {
            // Order by some named (via string) property.
            // NOTE: This will throw if the column/propertyName doesn't exist !

            Type entityType = typeof(T);
            PropertyInfo prop1 = entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy); // JavaScript can kill case.

#if false
            // NOTE: This doesn't work for EF 3.1 for MySQL!!!!!!!!!!!!
            ParameterExpression param1 = Expression.Parameter(entityType, string.Empty); // I don't care about some naming?
            MemberExpression property = Expression.PropertyOrField(param1, prop1.Name);    // T for prop. propertyName case not sensitive.
#else
            // Create x=>x.PropName; //  Expression<Func<AgencySystemBiz, int>>
            ParameterExpression param1 = Expression.Parameter(entityType, "x");
            MemberExpression property = Expression.Property(param1, prop1.Name);
#endif

            Expression orderByExp = Expression.Lambda(property, param1);

            return OrderingHelperX(source, prop1.PropertyType, orderByExp, isSortAscending, thenByLevel);
        }

        public static IOrderedQueryable<T> OrderByX<T>(this IQueryable<T> source, string propertyName, bool isSortAscending = true)
        {
            // return OrderingHelper3_TEST(source, propertyName);
            return OrderingHelper1(source, propertyName, isSortAscending, false);
        }

        public static IOrderedQueryable<T> ThenByX<T>(this IOrderedQueryable<T> source, string propertyName, bool isSortAscending = true)
        {
            // return OrderingHelper3_TEST(source, propertyName);
            return OrderingHelper1(source, propertyName, isSortAscending, true);
        }

        public static IOrderedQueryable<T> OrderByList1<T>(this IQueryable<T> source, IEnumerable<ComparerDef> sorts)
        {
            // Order by some named property(s). use reflection to get property type.
            // NOTE: This will throw if the column/propertyName doesn't exist !

            IOrderedQueryable<T> ret2 = null;
            foreach (var sort in sorts)
            {
                ret2 = OrderingHelper1(ret2 ?? source, sort.PropName, sort.SortDir == SortDirection.Ascending, ret2 != null);
            }

            return ret2;
        }

        //***********************************//

        public static IOrderedQueryable<T> OrderByList2<T>(this IQueryable<T> source, IEnumerable<ComparerDef> sorts, Func<string, Expression> propExp)
        {
            // Order by some named property(s). use IPropertyExpression to get property type.
            // NOTE: This will throw if the column/propertyName doesn't exist !

            Type entityType = typeof(T);

            IOrderedQueryable<T> ret2 = null;
            foreach (var sort in sorts)
            {
                PropertyInfo prop1 = entityType.GetProperty(sort.PropName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy); // JavaScript can kill case.
                Expression orderByExp = propExp(prop1.Name);    // call GetPropertyExp
                if (orderByExp == null)
                {
                    // this should not happen!
                    return ret2;
                }
                ret2 = OrderingHelperX(ret2 ?? source, prop1.PropertyType, orderByExp, sort.SortDir == SortDirection.Ascending, ret2 != null);
            }

            return ret2;
        }

    }

    [Serializable]
    public class DataPageRsp
    {
        // Response set for a data page.
        // Some Service returns the set of data i want. from DataPageReq

        public System.Collections.IList Rows { get; set; }     // The data rows requested for CurrentPage. un-typed.
        public int RowsTotal { get; set; }          // Total rows in the request. ignore paged result. All pages. ASSUME RowsTotal >= Rows.Length

        public void UpdateRowsTotal()
        {
            // SetRowsTotal
            this.RowsTotal = this.Rows.Count;
        }
    }

    public class DataPageRsp<T> : DataPageRsp
    {
        // Response set for a data page.
        // Rows cast to type T

        public T GetRow(int i)
        {
            // default(T)
            return (T)Rows[i];
        }
        public List<T> GetRows()
        {
            if (Rows is List<T>)
                return (List<T>)Rows;
            return Rows.Cast<T>().ToList();
        }

        public void SetRowsTotal(DataPageReq req, IQueryable<T> q)
        {
            // Total rows from paged results.
            if (this.Rows.Count < req.PageSize || req.PageSize <= 0)    // we can infer total if not enough for PageSize = end.
            {
                this.RowsTotal = req.GetSkip() + this.Rows.Count;
            }
            else
            {
                // TODO: try to avoid a second round trip to populate RowsTotal !??
                this.RowsTotal = q.Count();     //  2 trips to db ??? TODO FIXME
            }
        }

        public void SetEmpty()
        {
            Rows = new List<T>();
        }
    }

    [Serializable]
    public class DataPageReq
    {
        // Params to Request a page of data.
        // overload this to add search filters.

        public int StartOfPage { get; set; }    // start of the current page. 0 based.
        public int PageSize { get; set; }       // Max number of rows on page.
        public List<ComparerDef> SortFields { get; set; }   // How to sort fields.

        public DataPageReq()
        {
        }
        public DataPageReq(int startOfPage, int pageSize, List<ComparerDef> sortFields)
        {
            StartOfPage = startOfPage;      // Make sure this is 0 based !
            PageSize = pageSize;
            SortFields = sortFields;
        }

        public int GetSkip()
        {
            ValidState.ThrowIf(StartOfPage < 0);    // assume PageSize > 0 ?
            return StartOfPage;
        }

        public int GetPagesTotal(int rowsTotal) // not used.
        {
            // How many total pages to fit this many total rows?

            if (PageSize <= 0)
                return 0;
            if (rowsTotal <= PageSize)
                return 1;
            int pagesTotal = rowsTotal / PageSize;
            if (pagesTotal % PageSize > 0)
                pagesTotal++;
            return pagesTotal;
        }

        public IQueryable<T> GetQuery<T>(IQueryable<T> q, Func<string, Expression> propExp)
        {
            // get Paging query.
            // RETURN: IOrderedQueryable<T> or not.
            // NOTE: The method 'Skip' is only supported for sorted input in LINQ to Entities. The method 'OrderBy' MUST be called before the method 'Skip'.

            if (this.SortFields != null && this.SortFields.Count > 0)
            {
                var q2 = q.OrderByList2(this.SortFields, propExp); // will return IOrderedQueryable
                if (q2 == null) // prop didnt exist ! this is bad.
                    return q;
                q = q2;
            }
            if (this.PageSize > 0)  // paging. assume IOrderedQueryable.
            {
                q = q.Skip(this.GetSkip()).Take(this.PageSize);
            }
            return q;
        }

        public DataPageRsp<T> GetRsp<T>(IQueryable<T> q, Func<string, Expression> propExp)
        {
            // Query and return the rows for the current page. NOT async.
            var rsp = new DataPageRsp<T>();
            rsp.Rows = GetQuery(q, propExp).ToList(); // Lose type <T>.
            rsp.SetRowsTotal(this, q);
            return rsp;
        }
    }

    [Serializable]
    public class DataPageFilter : DataPageReq
    {
        // a data page with extra search/filter params imposed.
        // overload this to add more search filters.

        public string SearchFilter { get; set; }    // Filter on some important text field(s). Which ?? 
        public int FilterId { get; set; }           // A custom selection of predefined filters. enum. 0 = unused.  // optional.

        public DataPageFilter()
        {
        }
        public DataPageFilter(int startOfPage, int pageSize, List<ComparerDef> sortFields, string searchFilter = null, int filterId = 0)
            : base(startOfPage, pageSize, sortFields)
        {
            SearchFilter = searchFilter;
            FilterId = filterId;
        }
    }
}
