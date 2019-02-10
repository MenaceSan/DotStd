using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DotStd
{
    public enum DbVendor
    {
        // Db Vendors i might support.
        Unknown = 0,        // ODBC Connection ?
        SqlServer = 1,  // Microsoft,  MsSQL, M$
        MySQL = 2,      // Or Aurora.
        Oracle = 3,
        SQLite = 4,

        // PostgreSQL
        // Mongo ?
    }

    public class DbTableUtil
    {
        // Helpers for a db table.

        public const string kTables = "tables.txt";
        public const string kExt = ".csv";
        public const string kNULL = "NULL";

        public static bool IsColIgnored(string colName)
        {
            // assume this column is ignored?
            // Prefix the col header name with "Ignored_" to ignore it.

            return colName.StartsWith("Ignored_") || colName.StartsWith("Ignore_");
        }

        public static bool IsColBool(string colName)
        {
            // assume this column is boolean?
            return (colName.StartsWith("Is") && char.IsUpper(colName[2]))
                || (colName.StartsWith("Has") && char.IsUpper(colName[3]))
                || (colName.StartsWith("Uses") && char.IsUpper(colName[4]))
                ;
        }

        public static void AddFile(List<string> files, string fileName)
        {
            // Make a list of files for tables.
            // None dupe.
            if (string.IsNullOrWhiteSpace(fileName))
                return;
            if (!files.Contains(fileName))
            {
                files.Add(fileName);
            }
        }

        public static void AddDir(List<string> files, string dir)
        {
            // Make a list of .csv files for tables.
            var directoryInfo = new DirectoryInfo(dir);
            foreach (var fileInfo in directoryInfo.GetFiles("*" + kExt))
            {
                AddFile(files, fileInfo.Name);
            }
        }

        private static void AddFilesIn(List<string> files, string dirName, StreamReader fileRead)
        {
            // Make a list of .csv files for tables.
            while (!fileRead.EndOfStream)
            {
                string fileName = fileRead.ReadLine();
                if (fileName.StartsWith(";"))
                    continue;
                if (fileName == "*")
                {
                    AddDir(files, dirName);
                }
                else if (!string.IsNullOrWhiteSpace(fileName))
                {
                    AddFile(files, fileName + kExt);
                }
            }
        }

        public static string AddFilesIn(List<string> files, string filePath)
        {
            // Make a list of files for tables.
            string dirName = Path.GetDirectoryName(filePath);

            using (var fileRead = new StreamReader(filePath, Encoding.UTF8))
            {
                AddFilesIn(files, dirName, fileRead);
            }

            return dirName;
        }

        public static void AddDirX(List<string> files, string dir)
        {
            string path = Path.Combine(dir, kTables);
            if (File.Exists(path))
            {
                AddFilesIn(files, path);
            }
            else
            {
                AddDir(files, dir);
            }
        }
    }
}
