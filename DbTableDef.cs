using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace DotStd
{
    /// <summary>
    /// Db Vendors i might support.
    /// </summary>
    public enum DbVendor
    {
        Unknown = 0,        // ODBC Connection ? maybe InMemory provider.
        SqlServer = 1,  // Microsoft,  MsSQL, M$
        MySQL = 2,      // Or Aurora. 
        Oracle = 3,
        SQLite = 4,

        // PostgreSQL
        // Mongo ? Maria ?
    }

    /// <summary>
    /// What is the nature of the data in this table ?
    /// </summary>
    public enum DbTableType
    {
        Const = 1,      // Contains only enum of fixed values that can never change. may have matching enum in code.
        Ext,        // similar to Hybrid but externally defined. e.g. Geo_*,
        Hybrid1,     // Contains some fixed values and can have new values added. ids can never change. never deleted.
        Hybrid2,     // Tables that have circular deps on Hybrid1
        Demo,       // Fully dynamic but pre-populated with demo data.
        Dynamic,    // Fully dynamic. No initial data expected.
    }

    /// <summary>
    /// Define meta data for column/field. AKA AppField
    /// </summary>
    public class DbColumnDef
    {
        public string _Name;            // The columns entity/symbolic camel case name that will be used by EF entities. 
        public Type _Type;      // Data type for field/column. from EF/Db. Converter.IsNullableType() ? Type.GetTypeCode

        public DbColumnDef(string name, Type type)
        {
            _Name = name; _Type = type;
        }
    }

    /// <summary>
    /// Define metadata for a table that will hold columns/fields.
    /// db table schema data.
    /// The fields can have FK relationships to other tables.
    /// might be defined in AppTable/app_table table and reference AppField/app_field.
    /// assume singular naming for tables.
    /// This can be related to LambdaExpression? GetOrderByExp(string name)
    /// </summary>
    public class DbTableDef
    {
        public const string kNULL = "NULL";     // encode a null value for a field.

        public string TableName;       // The db table name. snake case. used by MySQL.
        public string Name;      // The entity/symbolic camel case name that will be used by EF entities. can be derived from TableName

        public DbTableType TableType;       // What is the nature of the data in this table ? Const vs Dynamic?
        public List<DbColumnDef>? Columns;  // list of my columns/fields from DbColumnDef/AppField/app_field. [0] must be PK, enum from EF object meta via UpdateColNames()?

        public DbTableDef()
        {
            // EF/Serializable construct.
            TableName = default!;
            Name = default!;
            // TableType = ?;
      }

        public DbTableDef(string tableName, DbTableType tt)
        {
            TableName = tableName;
            Name = GetEntityName(tableName);
            TableType = tt;
        }

        /// <summary>
        /// resolve the list of column names with the names reflected from the EF object.
        /// </summary>
        public void UpdateColumnsAs(Type t)
        {
            // ASSUME first prop is PK.
            Columns = new List<DbColumnDef>();
            var props = t.GetProperties();
            foreach (var prop in props)
            {
                Columns.Add(new DbColumnDef(prop.Name, prop.PropertyType));
            }
        }

        /// <summary>
        /// assume this column/field is ignored by its name?
        /// Prefix the col header name with "Ignored_" to ignore it.
        /// </summary>
        /// <param name="colName"></param>
        /// <returns></returns>
        public static bool IsColIgnored(string colName)
        {
            return colName.StartsWith("Ignored_") || colName.StartsWith("Ignore_");
        }

        static readonly string[] _prefixBool = { "Is", "Can", "Has", "Use", "Uses" };

        /// <summary>
        /// assume this column/field is boolean by its name?
        /// </summary>
        /// <param name="colName"></param>
        /// <returns></returns>
        public static bool IsColBool(string colName)
        {
            foreach (string prefix in _prefixBool)
            {
                if (colName.StartsWith(prefix) && colName.Length > prefix.Length && char.IsUpper(colName[prefix.Length]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Convert EF entity name (CamelCase) to db table name (snake_case with _ ).
        /// </summary>
        /// <param name="entityName"></param>
        /// <returns></returns>
        public static string GetDbTableName(string entityName)
        {
            var sb = new StringBuilder();
            int i = 0;
            foreach (char ch in entityName)
            {
                if (char.IsUpper(ch))
                {
                    if (i != 0)
                        sb.Append("_");
                    sb.Append(char.ToLower(ch));
                }
                else
                {
                    sb.Append(ch);
                }
                i++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Convert db table name (snake_case with _ ) to EF entity name (CamelCase).
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string GetEntityName(string tableName)
        {
            bool cap = true;
            var sb = new StringBuilder();
            foreach (char ch in tableName)
            {
                if (ch == '_')
                {
                    cap = true;
                    continue;
                }
                sb.Append(cap ? char.ToUpper(ch) : ch); // convert snake case to camel.
                cap = false;
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// List of files in a directory.
    /// </summary>
    public class DbTableDir
    {
        public const string kTables = "tables.txt";  // list dependency orders in here.

        public List<string> TableNames = new List<string>();    // must be read in proper order. not alpha order.

        public void AddTableName(string tableName)
        {
            if (TableNames.Contains(tableName))   // * can give me dupes. ignore them.
                return;
            TableNames.Add(tableName);
        }

        /// <summary>
        /// Make a list of .csv files for tables.
        /// </summary>
        /// <param name="dirName"></param>
        public void AddDirFiles(string? dirName)
        {
            if (string.IsNullOrWhiteSpace(dirName))
                return;
            var directoryInfo = new DirectoryInfo(dirName);
            foreach (var fileInfo in directoryInfo.GetFiles("*" + FileUtil.kExtCsv))
            {
                AddTableName(Path.GetFileNameWithoutExtension(fileInfo.Name));
            }
        }

        /// <summary>
        /// Make a list of .csv files for tables. from 'tables.txt'
        /// </summary>
        /// <param name="dirName"></param>
        /// <param name="fileRead"></param>
        private void AddTablesIn(string? dirName, StreamReader fileRead)
        {
            while (!fileRead.EndOfStream)
            {
                string? tableName = fileRead.ReadLine();
                if (string.IsNullOrWhiteSpace(tableName) || tableName.StartsWith(";"))
                    continue;
                if (tableName == "*")
                {
                    AddDirFiles(dirName);
                }
                else
                {
                    AddTableName(tableName);
                }
            }
        }

        /// <summary>
        /// Make a list of files for tables. from 'tables.txt'
        /// </summary>
        /// <param name="filePath"></param>
        public string? AddTablesIn(string filePath)
        {
            string? dirName = Path.GetDirectoryName(filePath);
            using (var fileRead = new StreamReader(filePath, Encoding.UTF8))
            {
                AddTablesIn(dirName, fileRead);
            }
            return dirName;
        }

        /// <summary>
        /// read the 'tables.txt' file to get a list of the files i will want and what order.
        /// </summary>
        /// <param name="dirName"></param>
        public void AddDirTables(string dirName)
        {
            string path = Path.Combine(dirName, kTables);
            if (File.Exists(path))
            {
                AddTablesIn(path);
            }
            else
            {
                AddDirFiles(dirName);
            }
        }
    }

    /// <summary>
    /// a list of database tables enum by name.
    /// order by tablename. order by entityname??
    /// </summary>
    public class DbTableDefs
    {
        public readonly Dictionary<string, DbTableDef> Tables = new Dictionary<string, DbTableDef>();

        /// <summary>
        /// Get a table by table name. (NOT entity name)
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public DbTableDef? GetTable(string tableName)
        {
            if (Tables.TryGetValue(tableName, out DbTableDef? table))
                return table;
            return null;
        }

        public void SetTable(DbTableDef table)
        {
            Tables[table.TableName] = table;
        }

        public void AddTable(DbTableDef table)
        {
            ValidState.AssertTrue(!Tables.ContainsKey(table.TableName));
            Tables.Add(table.TableName, table);
        }
    }
}
