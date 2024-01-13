using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DotStd
{
    /// <summary>
    /// Helper for paging of lists of data for LINQ and EF. SQL.
    /// Allow deferred (server/db side) paging.
    /// </summary>
    public static class DataPage
    {
        /// <summary>
        /// Order by some LambdaExpression.
        /// This works for EF 3.1 for MySQL with properly formed Expression.
        /// e.g. Expression<Func<AgencySystemBiz, int>> orderByExp = (x => x.IdInt); q.OrderingHelper1X(typeof(int), orderByExp);
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="orderByExp"></param>
        /// <param name="isSortAscending">reverse?</param>
        /// <param name="thenByLevel">not first level</param>
        /// <returns></returns>
        public static IOrderedQueryable<T> OrderingHelperX<T>(this IQueryable<T> source, LambdaExpression orderByExp, bool isSortAscending = false, bool thenByLevel = false)
        {
            MethodCallExpression call = Expression.Call(
                 typeof(Queryable),
                 thenByLevel ? (isSortAscending ? nameof(Queryable.ThenBy) : nameof(Queryable.ThenByDescending)) : (isSortAscending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending)),
                 new Type[] { typeof(T), orderByExp.ReturnType },
                 source.Expression,
                 Expression.Quote(orderByExp));

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
        }

        //***********************************//

        /// <summary>
        /// Order by some named (via string) property.
        /// NOTE: This will throw if the column/propertyName doesn't exist !
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <param name="isSortAscending"></param>
        /// <param name="thenByLevel"></param>
        /// <returns></returns>
        private static IOrderedQueryable<T> OrderingHelper_BROKEN<T>(IQueryable<T> source, string propertyName, bool isSortAscending, bool thenByLevel)
        {
            Type entityType = typeof(T);
            // JavaScript can kill case.
            PropertyInfo prop1 = ValidState.GetNotNull(entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy), nameof(prop1));

#if false
            // NOTE: This doesn't work for EF 3.1 for MySQL!!!!!!!!!!!!
            ParameterExpression param1 = Expression.Parameter(entityType, string.Empty); // I don't care about some naming?
            MemberExpression property = Expression.PropertyOrField(param1, prop1.Name);    // T for prop. propertyName case not sensitive.
#else
            // Create x=>x.PropName; //  Expression<Func<AgencySystemBiz, int>>
            ParameterExpression param1 = Expression.Parameter(entityType, "x");
            MemberExpression property = Expression.Property(param1, prop1.Name);
#endif

            LambdaExpression orderByExp = Expression.Lambda(property, param1);

            return OrderingHelperX(source, orderByExp, isSortAscending, thenByLevel);
        }

        public static IOrderedQueryable<T> OrderByX_BROKEN<T>(this IQueryable<T> source, string propertyName, bool isSortAscending = true)
        {
            // return OrderingHelper3_TEST(source, propertyName);
            return OrderingHelper_BROKEN(source, propertyName, isSortAscending, false);
        }

        public static IOrderedQueryable<T> ThenByX_BROKEN<T>(this IOrderedQueryable<T> source, string propertyName, bool isSortAscending = true)
        {
            // return OrderingHelper3_TEST(source, propertyName);
            return OrderingHelper_BROKEN(source, propertyName, isSortAscending, true);
        }

        public static IOrderedQueryable<T>? OrderByList1_BROKEN<T>(this IQueryable<T> source, IEnumerable<ComparerDef> sorts)
        {
            // Order by some named property(s). use reflection to get property type.
            // NOTE: This will throw if the column/propertyName doesn't exist !

            IOrderedQueryable<T>? ret2 = null;
            foreach (var sort in sorts)
            {
                ret2 = OrderingHelper_BROKEN(ret2 ?? source, sort.PropName, sort.SortDir == SortDirection.Ascending, ret2 != null);
            }

            return ret2;
        }

        //***********************************//

        /// <summary>
        /// Order by some named property(s). use IPropertyExpression to get property type.
        /// NOTE: This will throw if the column/propertyName doesn't exist !
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="sorts"></param>
        /// <param name="getOrderBy">GetOrderByExp</param>
        /// <returns></returns>
        public static IOrderedQueryable<T>? OrderByList2<T>(this IQueryable<T> source, IEnumerable<ComparerDef> sorts, Func<string, LambdaExpression?> getOrderBy)
        {
            Type entityType = typeof(T);

            IOrderedQueryable<T>? ret2 = null;
            foreach (var sort in sorts)
            {
                // JavaScript can kill case.
                PropertyInfo prop1 = ValidState.GetNotNull(entityType.GetProperty(sort.PropName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy), 
                    nameof(prop1));
                ValidState.ThrowIf(!prop1.Name.Equals(sort.PropName, StringComparison.InvariantCultureIgnoreCase));    // NOTE: ISNT prop1.Name THE SAME AS sort.PropName ??? remove entityType.GetProperty??

                LambdaExpression? orderByExp = getOrderBy(prop1.Name);    // call GetOrderByExp()
                if (orderByExp == null)
                {
                    // this should not happen! don't allow sort by this prop ?
                    return ret2;
                }

                ret2 = OrderingHelperX(ret2 ?? source, orderByExp, sort.SortDir == SortDirection.Ascending, ret2 != null);
            }

            return ret2;
        }
    }

    /// <summary>
    /// Response set for a data page.
    /// Some Service returns the set of data i want. from DataPageReq
    /// </summary>
    [Serializable]
    public class DataPageRsp
    {
        public System.Collections.IList Rows { get; set; }     // The data rows requested for CurrentPage. un-typed.
        public int RowsOnPage => this.Rows.Count;
        public int RowsTotal { get; set; }          // Total rows in the request. All pages. ASSUME RowsTotal >= RowsOnPage, Rows.Length

        public void SetEmpty()
        {
            Rows.Clear();
            RowsTotal = 0;
        }
        protected DataPageRsp()
        {
            // EF/Serializable construct
            Rows = default!;
        }
    }

    public class DataPageRsp<T> : DataPageRsp
    {
        // Response set for a data page.
        // Rows cast to type T

        public DataPageRsp()
        {
            Rows = new List<T>();
        }

        public List<T> GetRows()
        {
            return (List<T>)Rows;
        }
        public T GetRow(int i)
        {
            return GetRows()[i];
        }

        public void SetRowsTotal(DataPageReq req, IQueryable<T> q)
        {
            // Total rows from paged results.

            int rowsOnPage = RowsOnPage;

            if (rowsOnPage < req.PageSize || req.PageSize <= 0)    // we can infer total if not enough for PageSize = end.
            {
                this.RowsTotal = req.GetSkip() + rowsOnPage;
            }
            else
            {
                // TODO: try to avoid a second round trip to populate RowsTotal !??
                this.RowsTotal = q.Count();     //  2 trips to db ??? TODO FIXME
            }
        }
    }

    [Serializable]
    public class DataPageReq
    {
        // Params to Request a page of data from a Db.
        // overload this to add search filters.

        public int StartOfPage { get; set; }    // start of the current page. 0 based.
        public int PageSize { get; set; }       // Max number of rows on page.
        public List<ComparerDef>? SortFields { get; set; }   // How to sort/filter fields.

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

        public IQueryable<T> GetQuery<T>(IQueryable<T> q, Func<string, LambdaExpression?> propExp)
        {
            // get Paging query.
            // RETURN: IOrderedQueryable<T> or not.
            // NOTE: The method 'Skip' is only supported for sorted input in LINQ to Entities. The method 'OrderBy' MUST be called before the method 'Skip'.

            if (this.SortFields != null && this.SortFields.Count > 0)
            {
                var q2 = q.OrderByList2(this.SortFields, propExp); // will return IOrderedQueryable
                if (q2 == null) // prop didn't exist ! this is bad.
                    return q;
                q = q2;
            }
            if (this.PageSize > 0)  // paging. assume IOrderedQueryable.
            {
                q = q.Skip(this.GetSkip()).Take(this.PageSize);
            }
            return q;
        }

        public DataPageRsp<T> GetRsp<T>(IQueryable<T> q, Func<string, LambdaExpression?> propExp)
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
        // I am setting up a query to a Db.
        // overload this to add more search filters.

        public string? SearchFilter { get; set; }    // Filter on some important text field(s). Which ?? 
        public int FilterId { get; set; }           // A custom selection of predefined filters. enum. 0 = unused.  // optional.

        public DataPageFilter()
        {
        }
        public DataPageFilter(int startOfPage, int pageSize, List<ComparerDef> sortFields, string? searchFilter = null, int filterId = 0)
            : base(startOfPage, pageSize, sortFields)
        {
            SearchFilter = searchFilter;
            FilterId = filterId;
        }
    }
}
