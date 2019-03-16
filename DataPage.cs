using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace DotStd
{
    public static class DataPage
    {
        // Helper for paging of lists of data for Linq and EF.
        // Allow deferred (server side) paging.

        private static IOrderedQueryable<T> OrderingHelper<T>(IQueryable<T> source, string propertyName, bool bSortAscending, bool thenByLevel)
        {
            // Order by some named property.
            // NOTE: This will throw if the column/propertyName doesn't exist !
            // if (string.IsNullOrEmpty(propertyName)) // do nothing?
            //    return source;

            ParameterExpression param = Expression.Parameter(typeof(T), string.Empty); // I don't care about some naming
            MemberExpression property = Expression.PropertyOrField(param, propertyName);    // case not sensitive.
            LambdaExpression sort = Expression.Lambda(property, param);
            MethodCallExpression call = Expression.Call(
                typeof(Queryable),
                (thenByLevel ? "ThenBy" : "OrderBy") + (bSortAscending ? string.Empty : "Descending"),
                new[] { typeof(T), property.Type },
                source.Expression,
                Expression.Quote(sort));
            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
        }

        public static IOrderedQueryable<T> OrderByX<T>(this IQueryable<T> source, string propertyName, bool bSortAscending = true)
        {
            // NOTE: This will throw if the column/propertyName doesn't exist !
            return OrderingHelper(source, propertyName, bSortAscending, false);
        }
        public static IOrderedQueryable<T> ThenByX<T>(this IOrderedQueryable<T> source, string propertyName, bool bSortAscending = true)
        {
            return OrderingHelper(source, propertyName, bSortAscending, true);
        }

        public static IOrderedQueryable<T> OrderByList<T>(this IQueryable<T> source, IEnumerable<ComparerDef> sorts)
        {
            // Order by some named property(s).

            IOrderedQueryable<T> ret2 = null;
            bool thenByLevel = false;
            foreach (var sort in sorts)
            {
                ret2 = OrderingHelper(ret2 ?? source, sort.PropName, sort.SortDir == SortDirection.Ascending, thenByLevel);
                thenByLevel = true;
            }

            return ret2;
        }
    }

    public class DataPageRsp
    {
        // Response set for a data page.
        // Some Service returns the set of data i want. from DataPageReq

        public System.Collections.IList Rows { get; set; }     // The data rows requested for CurrentPage. un-typed.
        public int RowsTotal { get; set; }          // Total rows in the request. ignore paged result. All pages. ASSUME RowsTotal >= Rows.Length

        public void UpdateRowsTotal()
        {
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

        public void SetRowsTotal(DataPageReq req, IQueryable<T> q)
        {
            // Total rows from paged results.
            if (this.Rows.Count < req.PageSize || req.PageSize <= 0)    // can we infer total? not enough for PageSize = end.
            {
                this.RowsTotal = req.GetSkip() + this.Rows.Count;
            }
            else
            {
                // TODO: try to avoid a second round trip to populate RowsTotal !??
                this.RowsTotal = q.Count();     //  2 trips to db ??? TODO FIXME
            }
        }
    }

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
            ValidState.ThrowIf(StartOfPage < 0 || PageSize <= 0);
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

        public IQueryable<T> GetQuery<T>(IQueryable<T> q)
        {
            // Paging query.
            // q = IOrderedQueryable<T>
            // NOTE: The method 'Skip' is only supported for sorted input in LINQ to Entities. The method 'OrderBy' must be called before the method 'Skip'.

            if (this.SortFields != null && this.SortFields.Count > 0)
            {
                q = q.OrderByList(this.SortFields);
            }
            if (this.PageSize > 0)  // paging.
            {
                q = q.Skip(this.GetSkip()).Take(this.PageSize);
            }
            return q;
        }

        public DataPageRsp<T> GetRsp<T>(IQueryable<T> q)
        {
            // Query and return the rows for the current page.
            var rsp = new DataPageRsp<T>();
            rsp.Rows = GetQuery(q).ToList(); // Lose type <T>.
            rsp.SetRowsTotal(this, q);
            return rsp;
        }
    }

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
