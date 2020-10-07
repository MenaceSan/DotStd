using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DotStd
{
    public class CdnHost
    {
        // An external host that we use that may be allowed or disallowed. (has fallback)
        // we can also add any external services like Google Map APIs that may be enabled or disabled dynamically?

        // TODO Manage list of CDN services such that they may be enabled/disabled on the fly.
        //   remove CDN hosts known to be bad ? fallback to next alternate.

        public readonly string HostName;         // all files from this source.
        public bool Enabled;        // If we know its not working, we should disable it and take a backup. or fallback to local.

        public CdnHost(string hostname)
        {
            this.HostName = hostname;
            Enabled = true;
        }
    }

    public enum CdnRet
    {
        // Result of trying to sync a CDN resource.
        Error = -1,
        Valid = 0,   // already valid. do nothing.
        Updated = 1,
    }

    public enum CdnTagName
    {
        ERROR,    // Could not find any files for the ref.
        script,     // <script src="name.js" /> // javascript
        link,       // <link rel="stylesheet" href="sdfsdf.css" />
        a,          // <a href="name" /> // Some other resource.
    }

    public class CdnResource
    {
        // An object that may be included as js, css, font, or image
        // Available locally or via CDN.
        // allow listing of LOCAL ONLY resources that might need to be minified.


        public const string kDataMinOnlyAttr = "data-minonly";         // The dev/debug version of this file does not exist! ONLY minified.
        public const string kDataLibAttr = "data-lib";         // LibMan destination directory for equivalent file. NOT USED.

        public readonly CdnTagName TagName;     // TagName = element tag name for type. (or ERROR)
        public string AttrSrc => (this.TagName == CdnTagName.script) ? "src" : "href"; // different attr if JavaScript vs CSS/a ?  e.g. <script> JS file using "src" else <a> or <link> uses "href"

        public readonly string name;    // short name or Local path/name for the file. 
        public readonly string fallback_src;   // Local name/path. asp-fallback-src="/cdn/dropzone/min/dropzone.min.js". unique. usually minified. ALL MUST have this!

        public string integrity;    // integrity="sha256-cs4thShDfjkqFGk5s2Lxj35sgSRr4MRcyccmi0WKqCM=". unique for minified version. for CDN access ONLY. 
        public readonly string map;          // name (NO path info) of a map file. The minified version can use a map file for debug. "data-map", null = no map supplied. 

        // NOTE: multiple alternate CDNs ? or is this overkill?
        public readonly string CdnPath1;      // primary path to the minified CDN file. e.g. href or src="https://cdnjs.cloudflare.com/ajax/libs/dropzone/5.5.1/min/dropzone.min.js". MUST exist.
        public CdnHost CdnHost1;    // host for CdnPath1
        public bool IsCdnEnabled => CdnHost1?.Enabled ?? false;     // assumes CdnPath1

        public readonly string fallback_test;    // for js <script>(fallback_test||document.write("<script>alternate include </script>"))</script> AKA "asp-fallback-test" or "asp-fallback-test-class"
        public readonly string fallback_test_prop;   // CSS asp-fallback-test-property="position" 
        public readonly string fallback_test_val;    // CSS asp-fallback-test-value=""

        public readonly bool minonly;      // No non minified version is available for some reason. "data-minonly". only has minified. dont look for non minified version.
        public readonly string lib;             // path to libman local install. 'data-lib'="/lib/dropzone/dist"   // NOT USED.
        public readonly string version;      // For local files ONLY. Equiv to "asp-append-version". Arbitrary value that is used to break client side cache. similar to integrity

        // has pre-requisites? list of dependencies/requires i need to work.  
        public List<CdnResource> Requires;      // "data-req"

        // CdnCssTest must define this function ONLY ONCE in the file before using CDN CSS includes.
        // a = asp-fallback-test-property='position' 
        // b = asp-fallback-test-value='absolute'
        // c = replacement links
        // d = rel='stylesheet' crossorigin='anonymous'

        public const string kCssTestName = "CdnCssTest";
        public const string kCssTestScript = "<script>function CdnCssTest(a,b,c,d){var e,f=document,g=f.getElementsByTagName('SCRIPT'),h=g[g.length-1].previousElementSibling,i=f.defaultView&&f.defaultView.getComputedStyle?f.defaultView.getComputedStyle(h):h.currentStyle;if(i&&i[a]!==b)for(e=0;e<c.length;e++)f.write('<link href=\"'+c[e]+'\" '+d+'/>')}</script> ";
        public const string kCssExtraAttr = "rel='stylesheet'";


        public string GetFallbackScript()
        {
            // Test if the CDN failed and take some action.
            // MUST be AFTER the <script src> or <link href css>
            Debug.Assert(fallback_src != null && this.fallback_test != null);

            string extraAttr = "";

            if (TagName == CdnTagName.link)
            {
                extraAttr = kCssExtraAttr;
                if (this.fallback_test_prop != null)
                {
                    // NOTE: This requires extra stuff too. kCssTestScript
                    return $"<meta name='x-stylesheet-fallback-test' content='' class='{this.fallback_test}'/><script>{kCssTestName}('{this.fallback_test_prop}','{this.fallback_test_val}',['{this.fallback_src}'],\"{extraAttr}\")</script>";
                }
            }

            // e.g. <script>(window.jQuery||document.write("\u003Cscript src=\u0022/cdn/jquery/jquery.min.js\u0022 \u003E\u003C/script\u003E"));</script>
            return $"<script>({this.fallback_test}||document.write('\\u003C{TagName} {AttrSrc}=\"{this.fallback_src}\"{extraAttr}\\u003E\\u003C/{TagName}\\u003E'))</script>";
        }

        public string GetLocalSrc(bool useDevVersion)
        {
            string localSrc = (!useDevVersion || this.minonly || !Minifier.IsMinName(this.fallback_src)) ? this.fallback_src : Minifier.GetNonMinName(this.fallback_src);
            if (this.version != null)
            {
                // version For local files. Equiv to "asp-append-version"
                localSrc += "?v=" + this.version;
            }
            return localSrc;
        }

        public string GetAddr(bool useCdn, bool useDevVersion)
        {
            // Get Url for some CDN resource that is not a typical script or CSS link.
            // Should we use a particular CDN server ? else fallback to local or some other server? 
            // This may be used to check for enable of external service API. Google Maps, etc.
            if (useCdn && this.IsCdnEnabled)
                return this.CdnPath1;
            return GetLocalSrc(useDevVersion);
        }

        public static string ReadAttr(XElement xl, params string[] names)
        {
            // attributes can have multiple alternate names.
            foreach (string name in names)
            {
                XAttribute dst = xl.Attribute(name);
                if (dst != null)
                {
                    return dst.Value;
                }
            }
            return null;
        }

        public static bool IsUsedTagName(string elemTagName)
        {
            return EnumUtil.IsMatch<CdnTagName>(elemTagName);
        }

        public static bool IsUsedElement(XElement xl)
        {
            return IsUsedTagName(xl?.Name?.LocalName);
        }

        const string kIntegrityAlgDef = HashUtil.kSha256;        // for integrity

        private static async Task<string> GetFileHash(string dstPath, string alg)
        {
            // compute a hash.
            var hasher = new HashUtil(HashUtil.FindHasherByName(alg));
            byte[] hashCode2 = await hasher.GetHashFileAsync(dstPath);
            Debug.Assert(hashCode2 != null);
            return Convert.ToBase64String(hashCode2);
        }

        private async Task UpdateIntegrity(string dstPath)
        {
            // Generate integrity if its missing. It should NOT be !
            // Calculate and display an integrity value to be added. We should add this to CdnAll

            if (TagName == CdnTagName.a)    // integrity does nothing for this type?
                return;
            if (this.CdnPath1 == null)
                return;

            string hash = await GetFileHash(dstPath, kIntegrityAlgDef);
            integrity = kIntegrityAlgDef + "-" + hash;

            // Print it out to log so i can update CdnAll manually!!
            LoggerUtil.DebugException(dstPath + " integrity='" + integrity + "'", null);
        }


        public async Task<CdnRet> SyncFile(string dstPath)
        {
            // Get a file from CDN if i don't already have it and check its integrity (if supplied)
            // can throw.

            byte[] hashCode1 = null;
            HashUtil hasher = null;

            var fi = new FileInfo(dstPath);
            if (integrity == null)
            {
                // integrity doesn't exist so just check that the file exists locally.
                if (fi.Exists && fi.Length > 0)
                {
                    await UpdateIntegrity(dstPath);     // suggest that we manually update integrity.
                    return CdnRet.Valid;    // it exists. good enough since we don't have integrity attribute.
                }

                // It doesn't exist locally. pull a local copy from CDN. No big deal.
            }
            else
            {
                // test local file integrity hash e.g. "sha256-", "sha384-"
                int i = integrity.IndexOf('-');
                if (i <= 0)     // badly formed integrity ?? Fail ?
                {
                    // This is bad !! i cant really fix this. fix it manually.
                    LoggerUtil.DebugException($"Bad CDN integrity format '{integrity}'", null);
                    return CdnRet.Error;
                }

                hashCode1 = Convert.FromBase64String(integrity.Substring(i + 1));
                hasher = new HashUtil(HashUtil.FindHasherByName(integrity));
                if (fi.Exists)
                {
                    // Does current file match integrity?
                    byte[] hashCode2 = await hasher.GetHashFileAsync(dstPath);
                    Debug.Assert(hashCode2 != null);

                    // string debugHash2 = Convert.ToBase64String(hashCode2);
                    if (ByteUtil.CompareBytes(hashCode1, hashCode2) == 0)     // integrity match is good. Skip this.
                        return CdnRet.Valid;

                    // local file DOES NOT MATCH integrity!!
                    // This really should never happen! Pull another file from the CDN and hope it matches.
                }
            }

            if (this.CdnPath1 == null)
            {
                // this file has no CDN. So i can't do anything!

                if (Minifier.IsMinName(dstPath) && await Minifier.CreateMinified(dstPath))
                {
                    // Local only lacked a minified version. so i made one.
                    return CdnRet.Updated;
                }

                LoggerUtil.DebugException($"No File ({integrity}) and No CDN path for '{name}'", null);
                return CdnRet.Error;
            }

            // Pull/Get the file from CDN. 
            LoggerUtil.DebugEntry($"Get '{this.CdnPath1}'");
            var dl = new HttpDownloader(this.CdnPath1, dstPath);

            // CDN can get "OperationCanceledException: The operation was canceled."
            await dl.DownloadFileAsync(true);  // Assume directory is created on demand.

            if (integrity == null)
            {
                // destination file should exist now. Does it?
                var fi2 = new FileInfo(dstPath);
                if (!fi2.Exists || fi2.Length <= 0)
                {
                    LoggerUtil.DebugException("CDN file size 0 for " + dstPath, null);
                    return CdnRet.Error;
                }

                await UpdateIntegrity(dstPath);
            }
            else
            {
                // Now (re)test integrity for the file i just got!
                hasher.Init();
                byte[] hashCode2 = await hasher.GetHashFileAsync(dstPath);
                if (ByteUtil.CompareBytes(hashCode1, hashCode2) != 0)     // MUST match.
                {
                    // This is BAD. It should never happen!
                    string debugHash2 = Convert.ToBase64String(hashCode2);
                    LoggerUtil.DebugException($"CDN integrity hash does not match for '{dstPath}'. should be integrity='{debugHash2}'", null);
                    return CdnRet.Error;
                }
            }

            return CdnRet.Updated;       // got it.
        }

        public static string GetPhysPathFromWeb(string outDir, string url)
        {
            // Get app relative physical (local) path for file given its URL.
            // url = site relative URL path.
            // / = outDir (wwwroot)
            // ~/ = MVC app root ?
            // NO / = root of app?

            if (url.StartsWith(UrlUtil.kSep))
            {
                return outDir + url;
            }

            // just leave it?
            return url;
        }
        public async Task<CdnRet> SyncElement(string outDir)
        {
            // Test a <a>, <link> or <script> element. Pull it from CDN if it doesn't exist locally.
            // RETURN: CdnRet : 0 = no update required, 1 = pulled file. -1=error

            try
            {
                string dstPath = GetPhysPathFromWeb(outDir, this.fallback_src);     // Make a real (app relative) physical path for destination.

                CdnRet ret = await SyncFile(dstPath);
                if (ret <= CdnRet.Error)
                {
                    return ret;
                }

                // Does it have a .map file?
                string mapPath;
                if (this.map != null)
                {
                    mapPath = UrlUtil.ReplaceFile(dstPath, this.map); // replace name.
                    if (ret == CdnRet.Updated || !File.Exists(mapPath))
                    {
                        var dlMap = new HttpDownloader(UrlUtil.ReplaceFile(this.CdnPath1, this.map), mapPath);
                        await dlMap.DownloadFileAsync();
                    }
                }

                // Pull the non-minified (Dev) version as well. if it has one.
                if (this.CdnPath1 != null && !this.minonly && Minifier.IsMinName(this.CdnPath1))  // it is minified?
                {
                    string devName = Minifier.GetNonMinName(dstPath);
                    if (ret == CdnRet.Updated || !File.Exists(devName))
                    {
                        var dlDev = new HttpDownloader(Minifier.GetNonMinName(this.CdnPath1), devName);
                        await dlDev.DownloadFileAsync();
                    }
                }

                return ret;
            }
            catch
            {
                // This is only run at startup and failure is very bad! Should we allow the server to start at all ?
                return CdnRet.Error;
            }
        }

        public CdnResource(XElement xl)
        {
            // Read XML element that defines the resource from my CdnAll.html file.
            // ASSUME IsUsedType(xl)

            TagName = EnumUtil.ParseEnum<CdnTagName>(xl.Name.LocalName);

            name = xl.Attribute(nameof(name))?.Value; // my local name. MUST ALWAYS EXIST.
            CdnPath1 = xl.Attribute(AttrSrc)?.Value;        // (src or href)
            fallback_src = xl.Attribute("asp-fallback-" + AttrSrc)?.Value; // my local path (src or href). MUST ALWAYS EXIST.

            if (UrlUtil.IsLocalAddr(CdnPath1) && fallback_src == null)
            {
                // This is really a local only file. no CDN.
                fallback_src = CdnPath1;
                CdnPath1 = null;        // NOT a true CDN
            }
            if (name == null)
                name = fallback_src;
            if (fallback_src == null)
                fallback_src = name;

            ValidState.ThrowIfWhiteSpace(name, "CdnResource name");

            integrity = xl.Attribute(nameof(integrity))?.Value;       // has integrity hash ? All should. 
            minonly = xl.Attribute(kDataMinOnlyAttr) != null;    // Allow null default. some elements have no non-minified version!
            map = xl.Attribute("data-map")?.Value;
            lib = xl.Attribute(kDataLibAttr)?.Value;        // NOT USED.

            fallback_test = ReadAttr(xl, "asp-fallback-test", "asp-fallback-test-class");
            fallback_test_prop = xl.Attribute("asp-fallback-test-property")?.Value; // For CSS
            fallback_test_val = xl.Attribute("asp-fallback-test-value")?.Value;
        }

        public CdnResource(string _name, string _tagname, string outDir)
        {
            // create an element on the fly. Assume its a local file.
            TagName = EnumUtil.ParseEnum<CdnTagName>(_tagname);
            name = _name;
            fallback_src = name;

            string path1 = GetPhysPathFromWeb(outDir, name);
            string name2 = Minifier.GetNonMinName(name);
            string path2 = GetPhysPathFromWeb(outDir, name2);
            bool exists1 = File.Exists(path1);
            bool exists2 = File.Exists(path2);
            if (exists1)
            {
                minonly = !exists2;
            }
            else if (exists2)
            {
                name = name2;
                path1 = path2;
                minonly = true;
            }
            else
            {
                TagName = CdnTagName.ERROR;
                minonly = false;
                return;
            }

            // version For local files. Equiv to "asp-append-version" (would use SHA256)

            var hasher = new HashUtil(HashUtil.GetMD5());       // GetSHA256()
            byte[] hashCode2 = hasher.GetHashFile(path1);
            Debug.Assert(hashCode2 != null);
            version = Convert.ToBase64String(hashCode2);
        }
    }

    public class CdnUtil
    {
        // Sync/Pull .js and .css files from a CDN and make local copies for failover and dev purposes.
        // Assume this is a singleton.
        // Read a file called 'CdnAll.html' that contains all the links to my CDN files.
        // pull local version of these. to "asp-fallback-src" or "asp-fallback-href"
        // Similar function to Bower but centered on the CDN not the FULL dev packages.
        // NOTE: In the future read the 'libman.json' file to build this ?! libman doesn't contain 'integrity' at this time we can't just use libman directly. (2018)

        // can be used with <environment include="Development"> or IncludeRefTagHelper.

        public bool UseCdn { get; set; } = true;    // turn on/off ALL use of CDNs

#if DEBUG
        public bool UseDev { get; set; } = true;   // use the dev version of files. EnvironMode.DEV 
#else
        public bool UseDev { get; set; } = false;   // use the dev version of files. EnvironMode.DEV 
#endif

        private Dictionary<string, CdnResource> Resources = new Dictionary<string, CdnResource>();       // a list of declared resources.
        public Dictionary<string, CdnHost> Hosts = new Dictionary<string, CdnHost>();     // a list of CDN hosts i use in Resources.

        public const int kMaxConcurrentUpdates = 1;  // how many concurrent updates do we allow ?

        public CdnResource FindResource(string name, bool wildCard)
        {
            CdnResource res;
            if (Resources.TryGetValue(name, out res))
                return res;
            if (!wildCard)
                return null;

            // TODO: Allow wild card resources. for things like languages and country flags. (so i dont have to register them all)

            // Create  wildcard that is missing locally ? 

            return res;
        }

        public void AddResource(string name, CdnResource res, string reqs)
        {
            if (!string.IsNullOrWhiteSpace(reqs))   // "data-req"
            {
                res.Requires = new List<CdnResource>();
                foreach (string dep in reqs.Split())
                {
                    var resDep = FindResource(dep, false);
                    Debug.Assert(resDep != null);
                    res.Requires.Add(resDep);
                }
            }

            Debug.Assert(!Resources.ContainsKey(name));
            Resources.Add(name, res);
            if (name != res.fallback_src)
            {
                Debug.Assert(!Resources.ContainsKey(res.fallback_src));
                Resources.Add(res.fallback_src, res);
            }

            // Add the Cdn Hosts to our list.
            if (res.CdnPath1 != null)
            {
                // add CDN Host info.
                string host = UrlUtil.GetHostName(res.CdnPath1);
                CdnHost host1;
                if (!Hosts.TryGetValue(host, out host1))
                {
                    host1 = new CdnHost(host);
                    Hosts.Add(host, host1);
                }

                res.CdnHost1 = host1;
            }
        }

        public const string kCdnAllFile = "CdnAll.html";

        public void ClearCdn()
        {
            Resources = new Dictionary<string, CdnResource>();       // a list of declared resources.
            Hosts = new Dictionary<string, CdnHost>();     // a list of CDN hosts i use in Resources.
        }

        public async Task<int> InitCdnAsync(string cdnAllFilePath, string outDir)
        {
            // Make sure all my (local copy) CDN based resources are up to date.
            // Called ONCE at startup to read cdnAllFilePath. 
            // 1. Read the HTML/XML cdnAllFilePath file.
            // 2. Pull all files from the CDN that we want locally as backups/fallback.
            // 3. Write out the local stuff to outDir. e.g. "wwwroot/cdn"

            if (!File.Exists(cdnAllFilePath))       // get my list from here.
                return 0;

            int downloadCount = 0;
            int fileCount = 0;
            XDocument doc = XDocument.Load(cdnAllFilePath);     // TODO: Use HTML agility pack to deal with proper HTML (Not XML) encoding??

            var tasks = new List<Task<CdnRet>>();   // allow background/parallel loading.

            // pull all 'a', 'link' and 'script' elements
            foreach (XNode node in doc.DescendantNodes())
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;

                var xl = node as XElement;
                if (!CdnResource.IsUsedElement(xl))
                    continue;

                var res = new CdnResource(xl);
                AddResource(res.name, res, xl.Attribute("data-req")?.Value);
                tasks.Add(res.SyncElement(outDir));
                fileCount++;

                if (tasks.Count < kMaxConcurrentUpdates)
                    continue;

                await Task.WhenAny(tasks.ToArray());        // Do the work.

                for (int i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    if (task.IsCompleted)
                    {
                        if (task.Result == CdnRet.Updated)
                            downloadCount++;
                        tasks.RemoveAt(i);  // done.
                        i--;
                    }
                }

                Debug.Assert(tasks.Count < kMaxConcurrentUpdates);
            }

            await Task.WhenAll(tasks.ToArray());        // do this in parallel.

            foreach (var task in tasks)
            {
                Debug.Assert(task.IsCompleted);
                if (task.Result == CdnRet.Updated)
                    downloadCount++;
            }

            return downloadCount;
        }

        public string GetAddr(CdnResource res)
        {
            if (res == null)
                return null;
            return res.GetAddr(this.UseCdn, this.UseDev);
        }
    }
}
