﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace DotStd
{
    /// <summary>
    /// Enumerate days of the week for M$ SQL server.
    /// like Microsoft.VisualBasic.FirstDayOfWeek (where Sunday=1,Saturday=7)
    /// like MySQL DAYOFWEEK()
    /// NOT the same as .NET System.DayOfWeek (where Sunday=0,Saturday=6) 
    /// NOT JavaScript where Sunday is 0, Monday is 1,
    /// </summary>
    public enum SQLDay
    {
        Sunday = 1,
        Monday = 2,
        Tuesday = 3,
        Wednesday = 4,
        Thursday = 5,
        Friday = 6,
        Saturday = 7,
    }

    /// <summary>
    /// SQL features specific to M$ SQL
    /// Like SqlConnectionStringBuilder
    /// NOTE: DateTime.MinValue = "1/1/0001 12:00:00 AM", SqlDateTime.MinValue = DateTime(1753, 1, 1)
    /// </summary>
    public static class SqlMsUtil
    {
        public static readonly DateTime kSmallDateTimeMin = new(1900, 01, 01, 00, 00, 00);    // "1/1/1900 12:00:00 AM"
        public static readonly DateTime kSmallDateTimeMax = new(2079, 06, 06, 23, 59, 00);

        /// <summary>
        /// M$ SQL error codes we should look for. SqlClient.SqlException.Number
        /// http://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlexception.number.aspx
        /// </summary>
        public enum SQLExcepNum
        {
            ServerNotFound = 17,        // k_ErrServerNotFound
            DBNetworkError = 53,        // k_ErrDBNetworkError
            DBNetworkInstError = 258,   // A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: TCP Provider, error: 0 - The wait operation timed out.)
            StoredProcedureDoesNotExist = 2812,
            DatabaseNotFound = 4060,    // k_ErrDatabaseNotFound
            DBConnectionRefused = 10061,    // k_ErrDBConnectionRefused
            InvalidLogin = 18456,       // k_ErrInvalidLogin
        }

        /// <summary>
        /// Is <= value for SqlDateTime?
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static bool IsMinValue(DateTime dt)
        {
            if (dt <= kSmallDateTimeMin)
                return true;
            return false;
        }

        /// <summary>
        /// Can the SQL db store this date as smalldatetime ?
        /// minimum SQL server smalldatetime value, maximum SQL server smalldatetime value
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsValidSqlSmallDateTime(DateTime t)
        {
            if (t < kSmallDateTimeMin || t > kSmallDateTimeMax)
                return false;
            return true;
        }

        public static bool IsValidSqlSmallDateTime([NotNullWhen(true)] DateTime? t)
        {
            if (!t.HasValue)
                return false;
            return IsValidSqlSmallDateTime(t.Value);
        }

        public static DateTime ToValidSqlSmallDateTime(DateTime t)
        {
            // Is this date/time less than the min?
            if (t < kSmallDateTimeMin)
                return kSmallDateTimeMin;

            // Is this date/time greater than the max?
            if (t > kSmallDateTimeMax)
                return kSmallDateTimeMax;

            // This date/time is OK. round to whole minutes
            long ticks = (t.Ticks + (TimeSpan.TicksPerMinute / 2) + 1) / TimeSpan.TicksPerMinute;
            return new DateTime(ticks * TimeSpan.TicksPerMinute);
        }
        public static DateTime ToValidSqlSmallDateTime(DateTime? t)
        {
            if (!t.HasValue)
                return kSmallDateTimeMin;
            return ToValidSqlSmallDateTime(t.Value);
        }
        public static DateTime? ToValidSqlSmallDateTimeN(DateTime? t)
        {
            if (!t.HasValue)
                return null;
            return ToValidSqlSmallDateTime(t.Value);
        }

        /// <summary>
        /// Cant the SQL db store this date as datetime?
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsValidSqlDateTime(DateTime t)
        {
            try
            {
                if (t == DateTime.MinValue) // same as default(DateTime)
                    return false;
                System.Data.SqlTypes.SqlDateTime dsql = t;
                return true;    // didn't throw = ok.
            }
            catch
            {
                return false;
            }
        }
        public static bool IsValidSqlDateTime([NotNullWhen(true)] DateTime? t)
        {
            if (!t.HasValue)
                return false;
            return IsValidSqlDateTime(t.Value);
        }

        /// <summary>
        /// Get the equiv .NET CLR data type for the named SQL data type.
        /// http://msdn.microsoft.com/en-us/library/bb386947%28v=vs.110%29.aspx
        /// </summary>
        /// <param name="sSQLDataType"></param>
        /// <returns></returns>
        public static Type? GetDataType(string? sSQLDataType)
        {
            switch (sSQLDataType)
            {
                case null:
                    break;
                case "bigint":
                    return typeof(long);
                case "int": // 56
                    return typeof(int);
                case "smallint":
                    return typeof(short);
                case "tinyint":
                    return typeof(byte);
                case "bit":
                    return typeof(bool);
                case "decimal":
                case "numeric": // (10,4) in Fee
                case "money":
                case "smallmoney": // in Fee
                    return typeof(decimal); // DECIMAL(29,4)
                case "real":
                    return typeof(float);
                case "float":
                    return typeof(double);
                case "char":
                case "nchar":
                case "varchar":
                case "nvarchar":
                case "text":
                case "ntext":   // ForumMessage
                case "xml":
                    return typeof(string);
                case "datetime":
                case "smalldatetime":
                case "date":    // Porthos
                    return typeof(DateTime);
                case "time":
                    return typeof(TimeSpan);
                case "uniqueidentifier":
                    return typeof(System.Guid);
                case "datetimeoffset":
                    return typeof(DateTimeOffset);
                case "binary":
                case "varbinary":   // sysdiagrams
                    return typeof(byte[]);      // or System.Data.Linq.Binary ?

                // case "sql_variant": return typeof(object);
                // case "datetime2: return typeof(DateTime);
                // case "image": return typeof(object); // return typeof(System.Data.Linq.Binary); // dtproperties
  
                default:
                    break;
            }

            // We should do something about this ! What type is this ?
            return null;
        }
    }
}
