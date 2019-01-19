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
            MemberExpression property = Expression.PropertyOrField(param, propertyName);
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
            foreach( var sort in sorts)
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

        // TODO: try to avoid a second round trip to populate RowsTotal !
        public int RowsTotal { get; set; }          // Total rows in the request. All pages. RowsTotal >= Rows.Length

        public System.Collections.IList Rows { get; set; }     // The data rows requested for CurrentPage. un-typed.
    }

    public class DataPageRsp<T> : DataPageRsp
    {
        // Response set for a data page.
        // Rows cast to type T

        public T GetRow(int i)
        {
            // default(T)
            return (T) Rows[i];
        }
    }

    public class DataPageReq
    {
        // Params to Request a page of data.
        public int CurrentPage { get; set; }        // Which page do i want? 0 based.
        public int PageSize { get; set; }       // Max number of rows on page.
        public List<ComparerDef> SortFields { get; set; }   // How to sort fields.

        public DataPageReq(int currentPage, int pageSize, List<ComparerDef> sortFields)
        {
            CurrentPage = currentPage;
            PageSize = pageSize;
            SortFields = sortFields;
        }

        public int GetSkip()
        {
            return CurrentPage * PageSize;
        }

        public int GetPagesTotal(int rowsTotal)
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
 
        public DataPageRsp<T> GetRsp<T>(IQueryable<T> source)
        {
            // Query and return the rows for the current page.
            // source = IOrderedQueryable<T>
            // NOTE: The method 'Skip' is only supported for sorted input in LINQ to Entities. The method 'OrderBy' must be called before the method 'Skip'.
            var rsp = new DataPageRsp<T>();
            var lst = source
                .OrderByList(this.SortFields)
                .Skip(CurrentPage * PageSize)
                .Take(PageSize)
                .ToList();
            rsp.Rows = lst; // Lose type <T>.
            rsp.RowsTotal = source.Count();     //  2 trips to db ??? TODO FIXME
            return rsp;
        }
    }

    public class DataPageSearch : DataPageReq
    {
        // SearchFilter = Search / filter based on some text.

        public string SearchFilter { get; set; }    // Filter on some important field(s). Which ???

        public DataPageSearch(int currentPage, int pageSize, List<ComparerDef> sortFields, string searchFilter)
            :base(currentPage, pageSize, sortFields)
        {
            CurrentPage = currentPage;
            PageSize = pageSize;
            SortFields = sortFields;
            SearchFilter = searchFilter;
        }
    }
}
