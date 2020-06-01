using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DotStd
{
    public class CdnHost
    {
        // An external host that we use that may be allowed or disallowed. (has fallback)
        // we can also add any external services like Google Map APIs that may be enabled or disabled dynamically?

        public string Name;         // short name.
        public string HostName;     // prefix for files form this source.
        public bool Enabled;        // If we know its not working, we should disable it and take a backup.

        // TODO
    }

    public class CdnResource
    {
        // An object that may be included as js, css, font, or image

        public const string kDataNoDevAttr = "data-nodev";         // The dev version of this file does not exist! ONLY minified.
        public const string kDataLibAttr = "data-lib";         // LibMan destination directory for equivalent file. NOT USED.

        public string type;      // element type. script,link,a  e.g. <script> JS file using "src" else <a> or <link> uses "href"

        public string name;     // Local path/name for the file. asp-fallback-src="/cdn/dropzone/min/dropzone.min.js". unique. usually minified.
        public string integrity;       //  integrity="sha256-cs4thShDfjkqFGk5s2Lxj35sgSRr4MRcyccmi0WKqCM=". unique.

        public string srcCdn1;      // primary path to the minified CDN file. e.g. href or src="https://cdnjs.cloudflare.com/ajax/libs/dropzone/5.5.1/min/dropzone.min.js". MUST exist.
        public string srcMapName;     // name of a map file. The minified version can use a map file. "data-map", null = no map supplied.

        public bool HasNoDev;      // No dev version is avilable for some reson. "data-nodev". No need for map file for dev version.

        public string lib;             // path to libman local install. data-lib="/lib/dropzone/dist"   // NOT USED.

        public CdnResource(XElement xl)
        {
            type = xl.Name.LocalName;
            string typeExt = (this.type == "script") ? "src" : "href"; // different key if JavaScript or CSS/a ?

            XAttribute dst = xl.Attribute("asp-fallback-" + typeExt);       // Assume exists.
            if (dst == null)
            {
                dst = xl.Attribute("data-fallback-" + typeExt);     // <a> is used for fonts/images and other data.
            }
            name = dst?.Value;
            integrity = xl.Attribute("integrity")?.Value;       // has integrity hash ?

            srcCdn1 = xl.Attribute(typeExt)?.Value;

            HasNoDev = xl.Attribute(kDataNoDevAttr) != null;    // Allow null default. some elements have no non-minified version!
            srcMapName = xl.Attribute("data-map")?.Value;
            lib = xl.Attribute(kDataLibAttr)?.Value;

            if (name == null)
                name = srcCdn1;
            if (srcCdn1 == null)
                srcCdn1 = name;
        }
    }

    public class CdnUtil
    {
        // Sync/Pull .js and .css files from a CDN and make local copies for failover and dev purposes.
        // Read a file called 'CdnAll.html' that contains all the links to my CDN files.
        // pull local version of these. to "asp-fallback-src" or "asp-fallback-href"
        // Similar function to Bower but centered on the CDN not the FULL dev packages.
        // TODO read the 'libman.json' file to build this ?! Since libman doesn't contain 'integrity' at this time we can't just use libman directly. (2018)

        // TODO Manage list of CDN services such that they may be enabled/disabled on the fly.
        // TODO Black list of CDNs . What CDN hosts are known to be bad ? fallback to next alternate.

        public const string kMin = ".min.";     // is minified version ?
        public const string kMin2 = "-min.";    // alternate minified naming style.

        public const string kInstRoot = "wwwroot";  // When converting URL to install physical file path.

        // should i use a CDN at all ?
        // can be used with <environment include="Development">
        public bool UseCdn { get; set; } = true;

        public List<CdnResource> Resources = new List<CdnResource>();       // a list of declared resources.


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

        const int kError = -1;
        const int kValid = 0;   // already valid. do nothing.
        const int kUpdated = 1;

        private static async Task<int> SyncFile(string dstPath, string integrity, string srcUrl)
        {
            // Get a file if i dont already have it and check its integrity (if supplied)

            byte[] hashCode1 = null;
            HashUtil hasher = null;

            if (integrity == null)
            {
                // integrity doesn't exist so just check that the file exists locally.
                var fi = new FileInfo(dstPath);
                if (fi.Exists && fi.Length > 0)
                    return kValid;    // it exists. good enough since we don't have integrity attribute.
            }
            else
            {
                // test hash e.g. "sha256-", "sha384-"
                int i = integrity.IndexOf('-');
                if (i <= 0)     // badly formed integrity ?? Fail ?
                {
                    // This is bad !!
                    LoggerUtil.DebugException("Bad CDN integrity format", null);
                    return kError;
                }

                hashCode1 = Convert.FromBase64String(integrity.Substring(i + 1));
                hasher = new HashUtil(HashUtil.FindHasher(integrity));
                if (File.Exists(dstPath))
                {
                    // Does current file match integrity?
                    byte[] hashCode2 = await hasher.GetHashFileAsync(dstPath);
                    // debugHash2 = Convert.ToBase64String(hashCode2);
                    if (ByteUtil.CompareBytes(hashCode1, hashCode2) == 0)     // integrity match is good. Skip this.
                        return kValid;

                }
            }

            // Pull/Get the file from CDN. 
            LoggerUtil.DebugEntry($"Get '{srcUrl}'");
            var dl = new HttpDownloader(srcUrl, dstPath);

            // CDN can get "OperationCanceledException: The operation was canceled."
            await dl.DownloadFileAsync(true);  // Assume directory is created on demand.

            if (integrity == null)
            {
                // Does the destination file exist now ?
                var fi2 = new FileInfo(dstPath);
                if (!fi2.Exists || fi2.Length <= 0)
                {
                    LoggerUtil.DebugException("CDN file size 0 for " + dstPath, null);
                    return kError;
                }
            }
            else
            {
                // Now (re)test integrity!
                hasher.Init();
                byte[] hashCode2 = await hasher.GetHashFileAsync(dstPath);
                // debugHash2 = Convert.ToBase64String(hashCode2);
                if (ByteUtil.CompareBytes(hashCode1, hashCode2) != 0)     // MUST match.
                {
                    LoggerUtil.DebugException("CDN integrity hash does not match for " + dstPath, null);
                    return kError;
                }
            }

            return kUpdated;       // got it.
        }

        private static bool IsUsedElement(XElement xl)
        {
            string nameXl = xl.Name.LocalName;
            switch (nameXl)
            {
                case "script":      // <script src="" />
                case "link":        // <link rel="stylesheet" href="" />
                case "a":           // <a href="" />
                    return true;   //  filter just the elements i want.
                default:
                    return false;   // Ignore a <div> or other junk. this is a comment or something. ignore it.
            }
        }

        private static async Task<int> SyncElement(CdnResource res)
        {
            // pull all <a>, <link> and <script> elements
            // RETURN: 0 = no update required, 1 = pulled file. -1=error


            string dstPath = GetPhysPathFromWeb(res.name);     // Make a real (app relative) physical path for destination.
            string cdnUrl = res.srcCdn1;

            int ret = await SyncFile(dstPath, res.integrity, cdnUrl);
            if (ret <= kError)
            {
                return ret;
            }

            // Does it have a .map file?
            string mapPath;
            if (res.srcMapName != null)
            {
                mapPath = URL.GetDir(dstPath) + res.srcMapName;
                if (ret == kUpdated || !File.Exists(mapPath))
                {
                    var dlMap = new HttpDownloader(URL.GetDir(cdnUrl) + res.srcMapName, mapPath);
                    await dlMap.DownloadFileAsync();
                }
            }

            // Pull the non-minified (Dev) version as well. if it has one.
            if ( !res.HasNoDev && (cdnUrl.Contains(kMin) || cdnUrl.Contains(kMin2)))  // it is minified?
            {
                string devName = GetNonMin(dstPath);
                if (ret == kUpdated || !File.Exists(devName))
                {
                    var dlDev = new HttpDownloader(GetNonMin(cdnUrl), devName);
                    await dlDev.DownloadFileAsync();
                }
            }

            return ret;
        }

        const int kConcurrent = 5;

        public async Task<int> InitCdnAsync(string cdnAllFilePath, string outDir)
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

            var tasks = new List<Task<int>>();

            // pull all 'a', 'link' and 'script' elements
            foreach (XNode node in doc.DescendantNodes())
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                if (!IsUsedElement((XElement)node))
                    continue;

                var res = new CdnResource((XElement)node);

                tasks.Add(SyncElement(res));
                fileCount++;

                if (tasks.Count < kConcurrent)
                    continue;

                await Task.WhenAny(tasks.ToArray());

                for (int i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    if (task.IsCompleted)
                    {
                        if (task.Result == kUpdated)
                            downloadCount++;
                        tasks.RemoveAt(i);
                        i--;
                    }
                }

                Debug.Assert(tasks.Count < kConcurrent);
            }

            await Task.WhenAll(tasks.ToArray());

            foreach (var task in tasks)
            {
                Debug.Assert(task.IsCompleted);
                if (task.Result == kUpdated)
                    downloadCount++;
            }

            return downloadCount;
        }

        public bool UseCdnProvider(string relUrl)
        {
            // Use (or not) a particular CDN server ?
            // This may be used to check for enable of external service API. Google Maps, etc.

            if (!UseCdn)
                return false;

            return true;
        }

        public static string GetScript(string n)
        {
            // Get HTML to load a JavaScript file source. Supply integrity.
            // If CDN is enabled get it from there else get it from the local fallback/backup/failover.
            // e.g. <script src='xxx'></script>
            // Use minified versions?

            // TODO 

            return n;
        }

        public static string GetCss(string n)
        {
            // get HTML for CSS link. Supply integrity.
            // e.g. <link rel='stylesheet' href='xxx' />
            // If CDN is enabled get it from there else get it from the local fallback/backup/failover.

            // TODO 

            return n;
        }
    }
}
