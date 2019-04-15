using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotStd
{
    public static class SqlOraUtil
    {
        // SQL features specific to Oracle.

        public enum DbType
        {
            // Stub for Oracle support.
            Clob = 1
        }

        public class Parameter
        {
            // Stub for Oracle support.
            public DbType OracleDbType { get; set; }
        }
    }
}
