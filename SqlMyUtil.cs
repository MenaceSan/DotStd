using System.Text;

namespace DotStd
{
    /// <summary>
    /// SQL features specific to MySql.
    /// MySQL DAYOFWEEK() returns the week day number (1 for Sunday,2 for Monday …… 7 for Saturday )
    /// </summary>
    public static class SqlMyUtil
    {
        public const string kDateFormat = "yyyy-MM-dd";     // If we must express a date as a string for database purposes, format it like this. Try NOT to use this. use DateTime instead.

        public static string GetDeleteSelectSQL(string table, string select, bool safe)
        {
            var sb = new StringBuilder();
            if (!safe)
            {
                sb.Append("SET SQL_SAFE_UPDATES=0;");
            }
            sb.Append("DELETE FROM ");
            sb.Append(table);
            sb.Append(" WHERE ");
            sb.Append(select);
            if (!safe)
            {
                sb.Append(";SET SQL_SAFE_UPDATES=1;");
            }
            return sb.ToString();
        }
    }
}
