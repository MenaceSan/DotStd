using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Minify some client site script file that has comments etc.
    /// https://docs.microsoft.com/en-us/aspnet/core/client-side/bundling-and-minification?view=aspnetcore-3.1
    /// use for: CSS, HTML, JavaScript
    /// </summary>
    public static class Minifier
    {
        public const string kMin = ".min.";     // is minified version ?
        public const string kMin2 = "-min.";    // alternate minified naming style.

        public const string kExtJs = ".js";
        public const string kExtCss = ".css";

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
            kExtCss,
            FileUtil.kExtHtm,
            FileUtil.kExtHtml,
            kExtJs,
        };

        const string kCommentClose = "*/";

        public static async Task<bool> CreateMinified(string srcPath, string dstMinPath)
        {
            // create a minified version of a file. CSS, JS or HTML.
            try
            {
                var contents = await FileUtil.ReadAllLinesAsync(srcPath);

                // Filter the contents to make the minified file.
                using (var wr = File.CreateText(dstMinPath))
                {
                    bool commentOpen = false;
                    foreach (string line in contents)
                    {
                        // remove extra whitespace at start and end of lines.

                        string line2 = line.Trim();
                        if (commentOpen)
                        {
                            // Look for close of comment.
                            int j = line2.IndexOf(kCommentClose);
                            if (j < 0)
                                continue;
                            line2 = line2.Substring(j + kCommentClose.Length).Trim();
                            commentOpen = false;
                        }

                        // remove blank lines.
                        if (string.IsNullOrWhiteSpace(line2))
                            continue;
                        // remove whole line comments. 
                        if (line2.StartsWith("//"))
                            continue;

                        do_retest:
                        // remove /* Block Comment */
                        int i = line2.IndexOf("/*");
                        if (i >= 0)
                        {
                            string lineStart = line2.Substring(0, i).Trim();
                            int j = line2.IndexOf(kCommentClose, i);
                            if (j < 0)
                            {
                                // to next line.
                                commentOpen = true;
                                line2 = lineStart;
                            } 
                            else
                            {
                                commentOpen = false;
                                line2 = lineStart + line2.Substring(j + kCommentClose.Length).Trim();
                                goto do_retest;
                            }
                        }

                        // ?? remove end line // comments
                        if (string.IsNullOrWhiteSpace(line2))
                            continue;
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

        /// <summary>
        /// make minified versions of all the files in this directory if they don't already exist.
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        public static async Task UpdateDirectory(string dirPath, string? searchPattern = null)
        {
            var dir = new DirectoryInfo(dirPath);
            var files1 = ((searchPattern == null) ? dir.GetFiles() : dir.GetFiles(searchPattern)).Where(x => extensions.Contains(x.Extension));
            foreach (FileInfo info in files1)
            {
                string name = info.Name;
                if (IsMinName(name))
                    continue;
                // does the non min file have a valid minified version? must be ~newer   
                string dstMinPath;
                FileInfo? infoMin = files1.FirstOrDefault(x => GetNonMinName(x.Name) == name && x.Name != name);
                if (infoMin != null)
                {
                    // Min file exists.
                    if ((infoMin.LastWriteTimeUtc - info.LastWriteTimeUtc).Minutes >= -2)  // minified must be newer or close enough.
                        continue;
                    dstMinPath = infoMin.FullName;
                }
                else
                {
                    dstMinPath = Path.Combine(Path.GetDirectoryName(info.FullName) ?? string.Empty, Path.GetFileNameWithoutExtension(name) + ".min" + Path.GetExtension(name)); // kMin
                }

                LoggerUtil.DebugEntry($"Update minified file '{dstMinPath}'");

                await CreateMinified(info.FullName, dstMinPath);
            }
        }
    }
}
