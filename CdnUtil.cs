using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace DotStd
{
    public class CdnHost
    {
        // An external host that we use that may be allowed or disallowed. (has fallback)
        // we can also add any external services like Google Map apis that may be enabled or disabled dynamically?

        public string HostName;
        public bool Enabled;
    }

    public class CdnConfig
    {
        // TODO List of CDN servers to be used. (or none)

    }

    public static class CdnUtil
    {
        // Sync/Pull .js and .css files from a CDN and make local copies for failover and dev purposes.
        // Read a file called 'CdnAll.html' that contains all the links to my CDN files.
        // pull local version of these. to "asp-fallback-src" or "asp-fallback-href"
        // Similar function to Bower but centered on the CDN not the FULL dev packages.
        // TODO read the 'libman.json' file to build this ?! Since libman doesnt contain 'integrity' at this time we can't just use libman directly. (2018)

        // TODO Manage list of CDN services such that they may be enabled/disabled on the fly.
        // TODO Black list of CDNs . What CDN hosts are known to be bad ?

        public const string kAll = "CdnAll.html";       // Define all Cdn files i might use.

        public const string kDataLibAttr = "data-lib-";      // If i pulled the lib from Bower (for developers not really for CDN consumers)
        public const string kMin = ".min.";     // is minified version ?
        public const string kMin2 = "-min.";    // alternate minified naming style.

        // should i use a CDN at all ?
        // can be used with <environment include="Development">
        public static bool UseCdn { get; set; } = true;

        public const string kInstRoot = "wwwroot";  // When converting URL to install physical file path.

        public static string GetNonMin(string n)
        {
            // Try to find the NON-minified version of the file. If it has one.
            return n.Replace(kMin, ".").Replace(kMin2, ".").Replace("/min/", "/");
        }

        public static string GetPhysPathFromWeb(string url)
        {
            // Get app relative physical path for file given its URL.
            // url = site relative URL path.
            // / = wwwroot
            // ~/ = MVC app root ?
            // NO / = root of app?

            if (url.StartsWith("/"))
            {
                return kInstRoot + url;
            }

            // just leave it?
            return url;
        }

        public static int SyncCdn(string cdnAllFilePath, string outDir)
        {
            // Make sure all my (local copy) CDN based resources are up to date.
            // Called at startup. not async?
            // 1. Read the HTML/XML file kAll from Resource.
            // 2. Pull all files from the CDN that we want locally as backups/fallback.
            // 3. Write out the local stuff to outDir. e.g. "wwwroot/cdn"

            if (!File.Exists(cdnAllFilePath))       // get my list from here.
                return 0;

            int downloadCount = 0;
            int fileCount = 0;
            XDocument doc = XDocument.Load(cdnAllFilePath);     // TODO: Use HTML agility pack to deal with proper encoding??

            // pull all 'a', 'link' and 'script' elements
            foreach (XNode node in doc.DescendantNodes())
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                XElement xl = (XElement)node;
                string nameXl = xl.Name.LocalName;
                if (nameXl != "script" && nameXl != "link" && nameXl != "a")        // filter just the elements i want.
                    continue;

                string typeExt = (nameXl == "script") ? "src" : "href"; // Is JavaScript or CSS/a ?
                XAttribute src = xl.Attribute(typeExt);
                if (src == null)
                {
                    // weird ! Fail!
                    LoggerUtil.DebugException("Bad CDN element def", null);
                    continue;
                }

                fileCount++;
                XAttribute integrity = xl.Attribute("integrity");       // has hash ?
                XAttribute dstDev = xl.Attribute("data-dev-" + typeExt);    // Allow null default.
                XAttribute dst = xl.Attribute("asp-fallback-" + typeExt);       // Assume exists.
                if (dst == null)
                {
                    dst = xl.Attribute("data-fallback-" + typeExt);     // a is used for fonts/images and other data.
                }
                if (dst == null)
                {
                    if (dstDev == null)
                    {
                        LoggerUtil.DebugException("Bad CDN data def", null);
                        continue;   // We should not let this happen?
                    }
                    dst = dstDev;
                    dstDev = null;
                    integrity = null;
                }

                string dstPath = GetPhysPathFromWeb(dst.Value); // Make a real path.
                byte[] hashCode1 = null;
                HashUtil hasher = null;

                if (integrity == null)
                {
                    // integrity doesn't exist so just check that the file exists locally.
                    if (File.Exists(dstPath))
                    {
                        var fi = new FileInfo(dstPath);
                        if (fi != null && fi.Length > 0)
                            continue;   // it exists. good enough since we dont have integrity.
                    }
                }
                else
                {
                    // test hash e.g. "sha256-", "sha384-"
                    int i = integrity.Value.IndexOf('-');
                    if (i <= 0)     // badly formed integrity ?? Fail ?
                    {
                        // This is bad !!
                        LoggerUtil.DebugException("Bad CDN integrity", null);
                        continue;
                    }

                    hashCode1 = Convert.FromBase64String(integrity.Value.Substring(i + 1));
                    hasher = new HashUtil(HashUtil.FindHasher(integrity.Value));
                    if (File.Exists(dstPath))
                    {
                        // Is current file ok?
                        byte[] hashCode2 = hasher.GetHashFile(dstPath);
                        // debugHash2 = Convert.ToBase64String(hashCode2);
                        if (ByteUtil.CompareBytes(hashCode1, hashCode2) == 0)     // integrity match is good.
                            continue;
                        hasher.Init();
                    }
                }

                // Pull/Get the file from CDN. 
                downloadCount++;
                LoggerUtil.DebugEntry($"Get '{src.Value}'");
                var dl = new HttpDownloader(src.Value, dstPath);

                // CDN can get "OperationCanceledException: The operation was canceled."
                dl.DownloadFile(true);  // Assume dir is created on demand.

                if (integrity == null)
                {
                    // Does the destination file exist now ?
                    var fi = new FileInfo(dstPath);
                    if (fi == null || fi.Length <= 0)
                    {
                        throw new Exception("CDN file size 0 for " + dstPath);
                    }
                }
                else
                {
                    // Now test again!
                    byte[] hashCode2 = hasher.GetHashFile(dstPath);
                    // debugHash2 = Convert.ToBase64String(hashCode2);
                    if (ByteUtil.CompareBytes(hashCode1, hashCode2) != 0)     // MUST match.
                    {
                        throw new Exception("CDN integrity hash does not match for " + dstPath);
                    }
                }

                if (src.Value.Contains(kMin) || src.Value.Contains(kMin2))
                {
                    // Pull the non-minified (Dev) version as well. if it has one.
                    if (dstDev == null || dstDev.Value != dst.Value)
                    {
                        dstPath = (dstDev != null) ? GetPhysPathFromWeb(dstDev.Value) : GetNonMin(dstPath);
                        var dl2 = new HttpDownloader(GetNonMin(src.Value), dstPath);
                        dl2.DownloadFile();
                    }
                }
            }

            return downloadCount;
        }

        public static void AddCdnHost(string h, bool enable = true)
        {
            // Add a CDN host that i might optionally use.

        }

        public static bool UseCdnProvider(string relUrl)
        {
            // Use a particular Cdn server ?
            // This may be used to check for enable of external service API. Google Maps, etc.

            if (!UseCdn)
                return false;

            return true;
        }

        public static string GetScript(string n)
        {
            // Get Html to load a JavaScript file source. Supply integrity.
            // If Cdn is enabled get it from there else get it from the local fallback/backup/failover.
            // e.g. <script src='xxx'></script>
            // Use minified versions?

            return n;
        }

        public static string GetCss(string n)
        {
            // get Html for CSS link. Supply integrity.
            // e.g. <link rel='stylesheet' href='xxx' />
            // If Cdn is enabled get it from there else get it from the local fallback/backup/failover.

            return n;
        }
    }
}
