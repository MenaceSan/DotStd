using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotStd
{
    public static class Minifier
    {
        // https://docs.microsoft.com/en-us/aspnet/core/client-side/bundling-and-minification?view=aspnetcore-3.1
        // CSS Minifier
        // HTML Minifier
        // JavaScript Minifier

        public const string kMin = ".min.";     // is minified version ?
        public const string kMin2 = "-min.";    // alternate minified naming style.

        public static bool IsMinName(string n)
        {
            // Does this seem to be the name of a minified file name?
            if (n == null)
                return false;
            return n.Contains(kMin) || n.Contains(kMin2);
        }
        public static string GetNonMinName(string n)
        {
            // make the NON-minified version of the file name.  
            return n.Replace(kMin, ".").Replace(kMin2, ".").Replace("/min/", UrlUtil.kSep);
        }

        public static readonly string[] extensions =
        {
            ".css",
            ".htm",
            ".html",
            ".js",
        };

        public static async Task<bool> CreateMinified(string dstMinPath)
        {
            // create a minified version of a file. CSS, JS or HTML.
            try
            {
                string srcPath = GetNonMinName(dstMinPath);
                var contents = await FileUtil.ReadAllLinesAsync(srcPath);

                // Filter the contents to make the minified file.
                using (var wr = File.CreateText(dstMinPath))
                {
                    foreach (string line in contents)
                    {
                        // remove extra whitespace at start and end of lines.
                        string line2 = line.Trim();
                        // remove blank lines.
                        if (string.IsNullOrWhiteSpace(line2))
                            continue;
                        // remove whole line comments. 
                        if (line2.StartsWith("//"))
                            continue;
                        // remove end line // comments. 
                        // Block Comments?
                        await wr.WriteLineAsync(line2);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LoggerUtil.DebugException("CreateMinified", ex);
                return false;
            }
        }

        public static async Task MakeMinifiedFiles(string dirPath, string searchPattern)
        {
            // make minified versions of all the files in this directory if they don't already exist.

            var dir = new DirectoryInfo(dirPath);
            var files1 = dir.GetFiles(searchPattern).Where(x => extensions.Contains(x.Extension));
            foreach (FileInfo info in files1)
            {
                string name = info.Name;
                if (IsMinName(name))
                    continue;
                // does the non min file have a valid minified version? must be ~newer   
                string dstMinPath;
                FileInfo infoMin = files1.FirstOrDefault(x => GetNonMinName(x.Name) == name);
                if (infoMin != null)
                {
                    if ((infoMin.CreationTimeUtc - info.CreationTimeUtc).Minutes >= -3)  // newer or close enough.
                        continue;
                    dstMinPath = infoMin.FullName;
                }
                else
                {
                    dstMinPath = Path.Combine(Path.GetDirectoryName(info.FullName), Path.GetFileNameWithoutExtension(name) + kMin + Path.GetExtension(name));
                }
                await CreateMinified(dstMinPath);
            }
        }
    }
}
