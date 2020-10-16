using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DotStd
{
    public enum DbVendor
    {
        // Db Vendors i might support.
        Unknown = 0,        // ODBC Connection ? maybe InMemory provider.
        SqlServer = 1,  // Microsoft,  MsSQL, M$
        MySQL = 2,      // Or Aurora.
        Oracle = 3,
        SQLite = 4,

        // PostgreSQL
        // Mongo ?
    }

    public enum DbTableType
    {
        // What is the nature of the data in this table ?
        Const = 1,      // Contains only enum of fixed values that can never change. may have matching enum in code.
        Hybrid,     // Contains some fixed values and can have new values added. ids can never change. never deleted.
        Demo,       // Fully dynamic
        Ext,        // similar to Hybrid but externally defined.
        Dynamic,    // Fully dynamic. No initial data expected.
    }

    public class DbTableDef
    {
        // Define metadata for a table that will hold columns/fields.
        // db table schema data.
        // The fields can have FK relationships to other tables.
        // might be defined in AppTable/app_table table and reference AppField/app_field.
        // assume singular naming for tables.

        public const string kNULL = "NULL";     // encode a null value for a field.

        public string TableName;       // The db table name. snake case. used by MySQL.
        public string Name;      // The entity/symbolic camel case name that will be used by EF entities. can be derived from TableName

        public DbTableType Type;       // What is the nature of the data in this table ?
        public List<string> ColNames;       // list of my columns/fields from AppField/app_field

        public static bool IsColIgnored(string colName)
        {
            // assume this column/field is ignored by its name?
            // Prefix the col header name with "Ignored_" to ignore it.

            return colName.StartsWith("Ignored_") || colName.StartsWith("Ignore_");
        }

        public static bool IsColBool(string colName)
        {
            // assume this column/field is boolean by its name?
            return (colName.StartsWith("Is") && char.IsUpper(colName[2]))
                || (colName.StartsWith("Can") && char.IsUpper(colName[3]))
                || (colName.StartsWith("Has") && char.IsUpper(colName[3]))
                || (colName.StartsWith("Use") && char.IsUpper(colName[3]))
                || (colName.StartsWith("Uses") && char.IsUpper(colName[4]))
                ;
        }

        public static string GetTableName(string entityName)
        {
            // Convert EF entity name (CamelCase) to db table name (snake_case with _ ).
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

        public static string GetEntityName(string tableName)
        {
            // Convert db table name (snake_case with _ ) to EF entity name (CamelCase).

            bool cap = true;
            var sb = new StringBuilder();
            foreach (char ch in tableName)
            {
                if (ch == '_')
                {
                    cap = true;
                    continue;
                }
                sb.Append(cap ? char.ToUpper(ch) : ch);
                cap = false;
            }
            return sb.ToString();
        }

        public DbTableDef(string tableName, DbTableType tt)
        {
            TableName = tableName;
            Name = GetEntityName(tableName);
            Type = tt;
        }
        public DbTableDef()
        {
        }
    }

    public class DbTableDir
    {
        // List of files in a directory.
        public const string kTables = "tables.txt";  // list dependency orders in here.
        public const string kExt = ".csv";

        public List<string> TableNames = new List<string>();    // must be read in proper order. not alpha order.

        void AddTableName(string tableName)
        {
            if (TableNames.Contains(tableName))   // * can give me dupes. ignore them.
                return;
            TableNames.Add(tableName);
        }

        private void AddDirFiles(string dirName)
        {
            // Make a list of .csv files for tables.
            var directoryInfo = new DirectoryInfo(dirName);
            foreach (var fileInfo in directoryInfo.GetFiles("*" + kExt))
            {
                AddTableName(Path.GetFileNameWithoutExtension(fileInfo.Name));
            }
        }

        private void AddTablesIn(string dirName, StreamReader fileRead)
        {
            // Make a list of .csv files for tables. from 'tables.txt'
            while (!fileRead.EndOfStream)
            {
                string tableName = fileRead.ReadLine();
                if (tableName.StartsWith(";"))
                    continue;
                if (tableName == "*")
                {
                    AddDirFiles(dirName);
                }
                else if (!string.IsNullOrWhiteSpace(tableName))
                {
                    AddTableName(tableName);
                }
            }
        }

        private string AddTablesIn(string filePath)
        {
            // Make a list of files for tables. from 'tables.txt'
            string dirName = Path.GetDirectoryName(filePath);

            using (var fileRead = new StreamReader(filePath, Encoding.UTF8))
            {
                AddTablesIn(dirName, fileRead);
            }

            return dirName;
        }

        public void AddDirTables(string dirName)
        {
            // read the 'tables.txt' file to get a list of the files i will want and what order.

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

    public class DbTableDefs
    {
        // a list of database tables.
        // order by tablename. order by entityname??

        protected readonly Dictionary<string, DbTableDef> Tables = new Dictionary<string, DbTableDef>();

        public DbTableDef GetTable(string tableName)
        {
            // Get a table by table name. (NOT entity name)
            DbTableDef table;
            if (Tables.TryGetValue(tableName, out table))
                return table;
            return null;
        }

        public virtual void AddTable(DbTableDef table)
        {
            ValidState.AssertTrue(!Tables.ContainsKey(table.TableName));
            Tables.Add(table.TableName, table);
        }

        public DbTableDef GetOrAddTable(string tableName, DbTableType tt)
        {
            // add a table. No dupes.
            if (string.IsNullOrWhiteSpace(tableName))
                return null;
            DbTableDef table;
            if (Tables.TryGetValue(tableName, out table))
            {
                // its ok to change DbTableType ?
                return table;
            }
            table = new DbTableDef(tableName, tt);
            AddTable(table);
            return table;
        }
    }
}
