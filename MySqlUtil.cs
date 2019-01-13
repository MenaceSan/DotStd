using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotStd
{
    public static class MySqlUtil
    {
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
