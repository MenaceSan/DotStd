using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotStd
{
    public static class SqlMyUtil
    {
        // SQL features specific to MySql.
        // MySQL DAYOFWEEK() returns the week day number (1 for Sunday,2 for Monday …… 7 for Saturday )

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
