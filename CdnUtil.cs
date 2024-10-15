using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

namespace DotStd
{
    /// <summary>
    /// An external host that we use that may be allowed or disallowed. (has fallback)
    /// we can also add any external services like Google Map APIs that may be enabled or disabled dynamically?
    /// Manage list of CDN services such that they may be enabled/disabled on the fly. fallback to next alternate.
    /// </summary>
    public class CdnHost : ExternalService
    {
        string _HostName;       // from BaseURL
        public int Resources;      // How many CdnResource files reference this?

        public override string Name => _HostName;
        public override string BaseURL => UrlUtil.kHttps + _HostName;
        public override string Icon => "<i class='far fa-clone'></i>";

        public CdnHost(string hostName)
        {
            _HostName = hostName;
            IsConfigured = true;
        }
    }

    /// <summary>
    /// Result of trying to sync a CDN resource.
    /// </summary>
    public enum CdnRet
    {
        Error = -1,
        Valid = 0,   // already valid. do nothing.
        Updated = 1,
    }

    /// <summary>
    /// What type of CDN resource is this?
    /// </summary>
    public enum CdnTagName
    {
        ERROR,    // Could not find any files for the ref.
        script,     // <script src="name.js" /> // javascript
        link,       // <link rel="stylesheet" href="sdfsdf.css" />
        a,          // <a href="name" /> // Some other resource.
    }

    /// <summary>
    /// An object that may be included as js, css, font, or image on some CdnHost
    /// Available locally or via CdnHost.
    /// Allow listing of LOCAL ONLY resources that might need to be minified.
    /// </summary>
    public class CdnResource
    {
        public const string kDataMinOnlyAttr = "data-minonly";         // The dev/debug version of this file does not exist! ONLY minified.
        public const string kDataLibAttr = "data-lib";         // LibMan destination directory for equivalent file. NOT USED.

        public CdnTagName TagName;     // TagName = element tag name for type. (or ERROR)
        public string AttrSrc => (this.TagName == CdnTagName.script) ? "src" : "href"; // different attr if JavaScript vs CSS/a ?  e.g. <script> JS file using "src" else <a> or <link> uses "href"

        public readonly string name;    // short name or Local path/name for the file. MUST NOT BE null.
        public readonly string fallback_src;   // Local name/path. asp-fallback-src="/cdn/dropzone/min/dropzone.min.js". unique. usually minified. ALL MUST have this!

        public string? integrity;    // integrity="sha256-cs4thShDfjkqFGk5s2Lxj35sgSRr4MRcyccmi0WKqCM=". unique for minified version. for CDN access ONLY. 
        public readonly string? map;          // name (NO path info) of a map file. The minified version can use a map file for debug. "data-map", null = no map supplied. 

        // NOTE: multiple alternate CDNs ? or is this overkill?
        public readonly string? CdnPath1;      // primary full path to the minified CDN file. e.g. href or src="https://cdnjs.cloudflare.com/ajax/libs/dropzone/5.5.1/min/dropzone.min.js".  

        [MemberNotNullWhen(returnValue: false, member: nameof(CdnPath1))]
        public bool IsLocalOnly => CdnPath1 == null;    // File is not from CDN.

        public CdnHost? CdnHost1;            // parent host for CdnPath1

        [MemberNotNullWhen(returnValue: true, member: nameof(CdnHost1))]
        public bool IsCdnEnabled => CdnHost1?.IsEnabled ?? false;     // assumes CdnPath1 != null

        // client side dynamic fallback.
        public readonly string? fallback_test;    // for js <script>(fallback_test||document.write("<script>alternate include </script>"))</script> AKA "asp-fallback-test" or "asp-fallback-test-class"
        public readonly string? fallback_test_prop;   // CSS asp-fallback-test-property="position" 
        public readonly string? fallback_test_val;    // CSS asp-fallback-test-value=""

        public readonly string? lib;             // path to libman local install. 'data-lib'="/lib/dropzone/dist"   // NOT USED.

        public readonly bool minonly;      // No non minified version is available for some reason. "data-minonly". only has minified. dont look for non minified version.
        public string? version;      // For local files. (instead of integrity) Equiv to "asp-append-version". Arbitrary value that is used to break client side cache. similar to integrity GetMD5() 

        // has pre-requisites? list of dependencies/requires i need to work.  
        public List<CdnResource>? Requires;      // "data-req"

        // CdnCssTest must define this function ONLY ONCE in the file before using CDN CSS includes.
        // a = asp-fallback-test-property='position' 
        // b = asp-fallback-test-value='absolute'
        // c = replacement links
        // d = rel='stylesheet' crossorigin='anonymous'

        public const string kCssTestName = "CdnCssTest";
        public const string kCssTestScript = "<script>function CdnCssTest(a,b,c,d){var e,f=document,g=f.getElementsByTagName('SCRIPT'),h=g[g.length-1].previousElementSibling,i=f.defaultView&&f.defaultView.getComputedStyle?f.defaultView.getComputedStyle(h):h.currentStyle;if(i&&i[a]!==b)for(e=0;e<c.length;e++)f.write('<link href=\"'+c[e]+'\" '+d+'/>')}</script> ";
        public const string kCssExtraAttr = "rel='stylesheet'";

        /// <summary>
        /// Include this client side script to test for CDN health.
        /// if the CDN failed take some alternative action.
        /// MUST be AFTER the <script src> or <link href css>
        /// </summary>
        /// <returns></returns>
        public string GetFallbackScript()
        {
            Debug.Assert(this.fallback_test != null);

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

        /// <summary>
        /// Get link and version (cache breaker) for local file.
        /// </summary>
        /// <param name="useDevVersion"></param>
        /// <returns></returns>
        public string GetLocalSrc(bool useDevVersion)
        {
            string localSrc = (!useDevVersion || this.minonly || !Minifier.IsMinName(this.fallback_src)) ? this.fallback_src : Minifier.GetNonMinName(this.fallback_src);
            if (this.version != null)
            {
                // version For local files. Equiv to "asp-append-version". used as cache breaker.
                localSrc += "?v=" + this.version;
            }
            return localSrc;
        }

        /// <summary>
        /// Get URL for some CDN resource that is not a typical script or CSS link.
        /// Should we use a particular CDN server ? else fallback to local or some other server? 
        /// This may be used to check for enable of external service API. Google Maps, etc.
        /// </summary>
        /// <param name="useCdn"></param>
        /// <param name="useDevVersion"></param>
        /// <returns></returns>
        public string GetAddr(bool useCdn, bool useDevVersion)
        {
            if (useCdn && this.IsCdnEnabled && this.CdnPath1 != null)
                return this.CdnPath1;
            return GetLocalSrc(useDevVersion);
        }

        public static string? ReadAttr(XElement xl, params string[] names)
        {
            // attributes can have multiple alternate names.
            foreach (string name in names)
            {
                XAttribute? dst = xl.Attribute(name);
                if (dst != null)
                {
                    return dst.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// is this a tag name i know?
        /// </summary>
        /// <param name="elemTagName"></param>
        /// <returns></returns>
        public static bool IsUsedTagName([NotNullWhen(true)] string? elemTagName)
        {
            return EnumUtil.IsMatch<CdnTagName>(elemTagName);
        }

        public static bool IsUsedElement([NotNullWhen(true)] XElement? xl)
        {
            return IsUsedTagName(xl?.Name?.LocalName);
        }

        const string kIntegrityAlgDef = HashUtil.kSha256;        // for integrity

        /// <summary>
        /// compute a hash and return base64 string. (of hash)
        /// </summary>
        /// <param name="dstPath"></param>
        /// <param name="hasher"></param>
        /// <returns></returns>
        private static async Task<string> GetFileHash(string dstPath, HashUtil hasher)
        {
            byte[] hashCode2 = await hasher.GetHashFileAsync(dstPath);
            if (hashCode2 == null)
                return string.Empty;
            return Convert.ToBase64String(hashCode2);
        }
        private static async Task<string> GetFileHash(string dstPath, string alg)
        {
            // compute a hash.
            return await GetFileHash(dstPath, new HashUtil(HashUtil.FindHasherByName(alg)));
        }

        /// <summary>
        /// Get app relative physical (local) path for file given its URL.
        /// </summary>
        /// <param name="outDir"></param>
        /// <param name="url">site relative URL path.</param>
        /// <returns></returns>
        public static string GetPhysPathFromRel(string outDir, string url)
        {
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

        const char kIntegritySep = '-';

        public async Task<CdnRet> UpdateLocalOnly(string outDir)
        {
            // local file. this file has no CDN. 
            // outDir = ConfigBiz.kInstRootDir

            ValidState.AssertTrue(this.IsLocalOnly);  // local only file.
            string pathSrc = GetPhysPathFromRel(outDir, name);

            var fiSrc = new FileInfo(pathSrc);
            if (!fiSrc.Exists)
            {
                // Local file must exist!
                return CdnRet.Error;
            }

            string pathMin = GetPhysPathFromRel(outDir, fallback_src);
            CdnRet ret = CdnRet.Valid;
            if (!minonly)
            {
                // Does this have a minified file ? if not make one.
                // has the source changed since the minified file was made ? re-minify?
                var fiMin = new FileInfo(pathMin);
                if (!fiMin.Exists || (fiSrc.LastWriteTimeUtc > fiMin.LastWriteTimeUtc.AddSeconds(2)))   // add some slop.
                {
                    if (!await Minifier.CreateMinified(pathSrc, pathMin))
                    {
                        return CdnRet.Error;
                    }
                    ret = CdnRet.Updated;
                }
            }

            // cache breaking version For local files. Equiv to "asp-append-version" (would use SHA256)
            if (string.IsNullOrEmpty(version))
            {
                version = await GetFileHash(pathMin, HashUtil.GetMD5());  // GetSHA256()
            }

            return ret;
        }

        private async Task SuggestIntegrity(string dstPath)
        {
            // Generate integrity if its missing. It should NOT be !
            // Calculate and display an integrity value to be added. We should add this to CdnAll

            ValidState.AssertTrue(!this.IsLocalOnly);  // CDN only. don't bother if its a local only file.
            if (TagName == CdnTagName.a)    // integrity does nothing for this type? 'a'
                return;    // 

            string hash = await GetFileHash(dstPath, kIntegrityAlgDef);
            integrity = string.Concat(kIntegrityAlgDef, kIntegritySep, hash);

            // Print it out to log so i can update CdnAll manually!!
            LoggerUtil.DebugError(dstPath + " integrity='" + integrity + "'", null);
        }

        /// <summary>
        /// Get a file from CDN if i don't already have it and check its integrity (if supplied)
        /// can throw.
        /// </summary>
        /// <param name="dstPath"></param>
        /// <returns></returns>
        private async Task<CdnRet> SyncFile(string dstPath)
        {
            ValidState.AssertTrue(!this.IsLocalOnly);  // CDN only. don't bother if its a local only file.
            ValidState.ThrowIfNull(this.CdnPath1, nameof(this.CdnPath1));
            ValidState.ThrowIfNull(this.CdnHost1, nameof(this.CdnHost1));

            string? hashCode1 = null;
            HashUtil? hasher = null;

            var fi = new FileInfo(dstPath);
            if (integrity == null)
            {
                // integrity doesn't exist so just check that the file exists locally.
                if (fi.Exists && fi.Length > 0)
                {
                    await SuggestIntegrity(dstPath);     // suggest that we manually update integrity.
                    return CdnRet.Valid;    // it exists. good enough since we don't have integrity attribute.
                }

                // It doesn't exist locally. pull a local copy from CDN. No big deal.
            }
            else
            {
                // test local file integrity hash e.g. "sha256-", "sha384-"
                int i = integrity.IndexOf(kIntegritySep);
                if (i <= 0)     // badly formed integrity ?? Fail ?
                {
                    // This is bad !! i cant really fix this. fix it manually.
                    LoggerUtil.DebugError($"Bad CDN integrity format '{integrity}'", null);
                    return CdnRet.Error;
                }

                hashCode1 = integrity.Substring(i + 1);
                hasher = new HashUtil(HashUtil.FindHasherByName(integrity.Substring(0, i)));
                if (fi.Exists)
                {
                    // Does current file match integrity?
                    string hashCode2 = await GetFileHash(dstPath, hasher);
                    if (hashCode2 == hashCode1)     // integrity match is good. Skip this.
                        return CdnRet.Valid;

                    // This really should never happen! Pull another file from the CDN and hope it matches.
                    LoggerUtil.DebugError($"Local File ({dstPath}) does not match integrity!? re-fetching.", null);
                }
            }

            // Pull/Get the file from CDN. 
            LoggerUtil.DebugEntry($"Get '{this.CdnPath1}'");
            this.CdnHost1.UpdateTry();
            var dl = new HttpDownloader(this.CdnPath1, dstPath);

            // CDN can get "OperationCanceledException: The operation was canceled."
            await dl.DownloadFileAsync(true);  // Assume directory is created on demand.

            if (integrity == null)
            {
                // destination file should exist now. Does it?
                var fi2 = new FileInfo(dstPath);
                if (!fi2.Exists || fi2.Length <= 0)
                {
                    string errorMsg = "CDN file size 0 for " + dstPath;
                    LoggerUtil.DebugError(errorMsg, null);
                    this.CdnHost1.UpdateFailure(errorMsg);
                    return CdnRet.Error;
                }

                await SuggestIntegrity(dstPath);
            }
            else
            {
                // Now (re)test integrity for the file i just got!
                ValidState.ThrowIfNull(hasher, nameof(hasher));
                hasher.Init();
                string hashCode2 = await GetFileHash(dstPath, hasher);
                if (hashCode1 != hashCode2)     // MUST match.
                {
                    // This is BAD. It should never happen!
                    string errorMsg = $"CDN integrity hash does not match for '{dstPath}'. Local integrity='{string.Concat(kIntegrityAlgDef, kIntegritySep, hashCode2)}'";
                    LoggerUtil.DebugError(errorMsg, null);
                    this.CdnHost1.UpdateFailure(errorMsg);
                    return CdnRet.Error;
                }
            }

            this.CdnHost1.UpdateSuccess();
            return CdnRet.Updated;       // got it.
        }

        /// <summary>
        /// Test a 'a', 'link' or 'script' element. Pull it from CDN if it doesn't exist locally.
        /// Assume we just called AddResource();
        /// </summary>
        /// <param name="outDir">ConfigBiz.kInstRootDir</param>
        /// <returns>0 = no update required, 1 = pulled file. -1=error</returns>
        public async Task<CdnRet> SyncElement(string outDir)
        {
            try
            {
                if (IsLocalOnly)
                {
                    return await UpdateLocalOnly(outDir);
                }

                ValidState.ThrowIfNull(this.CdnPath1,nameof(this.CdnPath1));
                ValidState.ThrowIfNull(this.CdnHost1, nameof(this.CdnHost1));

                string dstPath = GetPhysPathFromRel(outDir, this.fallback_src);     // Make a real (app relative) physical path for destination.
                CdnRet ret = await SyncFile(dstPath);
                if (ret <= CdnRet.Error)
                {
                    return ret;
                }

                // Does it have an associated .map file?
                string mapPath;
                if (this.map != null)
                {
                    mapPath = UrlUtil.ReplaceFile(dstPath, this.map); // replace name.
                    if (ret == CdnRet.Updated || !System.IO.File.Exists(mapPath))
                    {
                        var dlMap = new HttpDownloader(UrlUtil.ReplaceFile(this.CdnPath1, this.map), mapPath);
                        await dlMap.DownloadFileAsync();
                        this.CdnHost1.UpdateSuccess();
                    }
                }

                // Pull the non-minified (Dev) version as well. if it has one.
                if (this.CdnPath1 != null && !this.minonly && Minifier.IsMinName(this.CdnPath1))  // it is minified?
                {
                    string devName = Minifier.GetNonMinName(dstPath);
                    if (ret == CdnRet.Updated || !System.IO.File.Exists(devName))
                    {
                        var dlDev = new HttpDownloader(Minifier.GetNonMinName(this.CdnPath1), devName);
                        await dlDev.DownloadFileAsync();
                        this.CdnHost1.UpdateSuccess();
                    }
                }

                return ret;
            }
            catch (Exception ex)
            {
                // This is only run at startup and failure is very bad! Should we allow the server to start at all ?
                this.CdnHost1?.UpdateFailure(ex.ToString());
                return CdnRet.Error;
            }
        }

        public CdnResource(XElement xl)
        {
            // Read XML element that defines the resource from my CdnAll.html file.
            // ASSUME IsUsedType(xl)

            TagName = EnumUtil.ParseEnum<CdnTagName>(xl.Name.LocalName);

            CdnPath1 = xl.Attribute(AttrSrc)?.Value;        // (src or href) MUST be defined. CdnHost1 will be set later.
            ValidState.ThrowIfWhiteSpace(CdnPath1, nameof(CdnPath1));

            string? fallback1 = xl.Attribute("asp-fallback-" + AttrSrc)?.Value; // my local path (src or href). 
            string? name1 = xl.Attribute(nameof(name))?.Value; // my local name. use fallback_src if not supplied.

            if (fallback1 == null && UrlUtil.IsLocalAddr(CdnPath1))
            {
                // This is really a local only file. no CDN.
                fallback1 = CdnPath1;
                CdnPath1 = null;        // NOT a true CDN. IsLocalOnly
            }
            if (name1 == null)
                name1 = fallback1;
            if (fallback1 == null)
                fallback1 = name1;

            ValidState.ThrowIfWhiteSpace(name1, "CdnResource name");     // MUST have name
            name = name1;
            ValidState.ThrowIfWhiteSpace(fallback1, nameof(fallback1));
            fallback_src = fallback1;

            integrity = xl.Attribute(nameof(integrity))?.Value;       // has integrity hash ? All should. 
            minonly = xl.Attribute(kDataMinOnlyAttr) != null;    // Allow null default. some elements have no non-minified version!
            map = xl.Attribute("data-map")?.Value;
            version = xl.Attribute("data-version")?.Value;
            lib = xl.Attribute(kDataLibAttr)?.Value;        // NOT USED.

            fallback_test = ReadAttr(xl, "asp-fallback-test", "asp-fallback-test-class");
            fallback_test_prop = xl.Attribute("asp-fallback-test-property")?.Value; // For CSS
            fallback_test_val = xl.Attribute("asp-fallback-test-value")?.Value;
        }

        public CdnResource(string _name, string? _tagname)
        {
            // create an Local element on the fly. Assume _name is a local minified file. NOT CDN.
            // <IncludeRef name="/js/grid_common.js" tagname="script" /> // use minified name to indicate it has one. can auto swap to debug if configured.
            // ASSUME UpdateLocalOnly1() will be called next.

            ValidState.ThrowIfWhiteSpace(_name, "CdnResource name");
            name = _name;

            if (string.IsNullOrWhiteSpace(_tagname))
            {
                // derive TagName type from the file extension.
                if (_name.EndsWith(Minifier.kExtJs))
                    TagName = CdnTagName.script;
                else if (_name.EndsWith(Minifier.kExtCss))
                    TagName = CdnTagName.link;
                else
                    TagName = CdnTagName.a;
            }
            else
            {
                TagName = EnumUtil.ParseEnum<CdnTagName>(_tagname); // must be valid.
            }

            minonly = Minifier.IsMinName(name) || (TagName != CdnTagName.script && TagName != CdnTagName.link);     // This file is all there is so do nothing more.

            if (minonly)
            {
                fallback_src = name;
            }
            else
            {
                // Create the min file name.
                string ext = Path.GetExtension(name);
                fallback_src = name.Substring(0, name.Length - ext.Length) + ".min" + ext;  // Add ".min."
            }
        }
    }

    /// <summary>
    /// Sync/Pull .js and .css files from a CDN and make local copies for failover and dev purposes.
    /// Assume this is a singleton.
    /// Read a file called 'CdnAll.html' that contains all the links to my CDN files.
    /// pull local version of these. to "asp-fallback-src" or "asp-fallback-href"
    /// Similar function to Bower but centered on the CDN not the FULL dev packages.
    /// NOTE: In the future read the 'libman.json' file to build this ?! libman doesn't contain 'integrity' at this time we can't just use libman directly. (2018)
    /// can be used with <environment include="Development"> or IncludeRefTagHelper.
    /// </summary>
    public class CdnUtil
    {
        public bool UseCdn { get; set; } = true;    // turn on/off ALL use of CDNs. Serve locally.

#if DEBUG
        public bool UseDev { get; set; } = true;   // use the dev (non minified) version of files. EnvironMode.DEV 
#else
        public bool UseDev { get; set; } = false;   // use the dev (non minified) version of files. EnvironMode.DEV 
#endif

        private readonly Dictionary<string, CdnResource> ResourcesByName = new();       // a list of declared resources by name
        private readonly Dictionary<string, CdnResource> ResourcesBySrc = new();       // a list of declared resources by name

        public readonly Dictionary<string, CdnHost> Hosts = new();     // a list of CDN hosts i use in Resources.

        public const int kMaxConcurrentUpdates = 1;  // how many concurrent updates do we allow ?

        public CdnResource? FindResource(string name, bool wildCard)
        {
            // Find CdnResource by its name (or src?)
            if (ResourcesByName.TryGetValue(name, out CdnResource? res))
                return res;
            if (!wildCard)
                return null;

            if (ResourcesBySrc.TryGetValue(name, out res))
                return res;

            // does the file exist? but isnt yet registered?
            // TODO: Allow wild card resources. for things like languages and country flags. (so i dont have to register them all)
            // Create wildcard that is missing locally ? 

            return res;
        }

        /// <summary>
        /// register a resource i might get from some CDN
        /// </summary>
        /// <param name="name"></param>
        /// <param name="res"></param>
        /// <param name="reqNames">this resource requires other resources to work.</param>
        public void AddResource(string name, CdnResource res, string? reqNames)
        {
            if (!string.IsNullOrWhiteSpace(reqNames))   // "data-req"
            {
                res.Requires = new List<CdnResource>();
                foreach (string resName in reqNames.Split())
                {
                    var resDep = FindResource(resName, false);
                    Debug.Assert(resDep != null);
                    res.Requires.Add(resDep);
                }
            }

            lock (this)
            {
                if (ResourcesByName.ContainsKey(name))  // already here !, race?
                    return;
                ResourcesByName.Add(name, res);
            }

            if (name != res.fallback_src)
            {
                Debug.Assert(!ResourcesBySrc.ContainsKey(res.fallback_src));
                ResourcesBySrc.Add(res.fallback_src, res);
            }

            // Add the Cdn Hosts to our list.
            if (res.CdnPath1 != null)   // IsLocalOnly
            {
                // add CDN Host info.
                string hostName = UrlUtil.GetHostName(res.CdnPath1);

                if (!Hosts.TryGetValue(hostName, out CdnHost? host1))
                {
                    host1 = new CdnHost(hostName);
                    Hosts.Add(hostName, host1);
                }

                res.CdnHost1 = host1;
                host1.Resources++;
            }
        }

        public const string kCdnAllFile = "CdnAll.html";

        public void ClearCdn()
        {
            ResourcesByName.Clear();
            ResourcesBySrc.Clear();       // a list of declared resources.
            Hosts.Clear();     // a list of CDN hosts i use in Resources.
        }

        /// <summary>
        /// Make sure all my (local copy) CDN based resources are up to date.
        /// Called ONCE at startup to read cdnAllFilePath. 
        /// 1. Read the HTML/XML cdnAllFilePath file.
        /// 2. Pull all files from the CDN that we want locally as backups/fallback.
        /// 3. Write out the local stuff to outDir. e.g. "wwwroot/cdn"
        /// </summary>
        /// <param name="cdnAllFilePath"></param>
        /// <param name="outDir"></param>
        /// <returns></returns>
        public async Task<int> InitCdnAllAsync(string cdnAllFilePath, string outDir)
        {
            if (!System.IO.File.Exists(cdnAllFilePath))       // get my list from here.
                return 0;

            int downloadCount = 0;
            int fileCount = 0;
            XDocument doc = XDocument.Load(cdnAllFilePath);     // TODO: Use HTML agility pack to deal with proper HTML (Not XML) encoding?

            var tasks = new List<Task<CdnRet>>();   // allow background/parallel loading.

            // pull all 'a', 'link' and 'script' elements
            foreach (XNode node in doc.DescendantNodes())
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                if (node is not XElement xl)
                    continue;
                if (!CdnResource.IsUsedElement(xl))
                    continue;

                var res = new CdnResource(xl);
                AddResource(res.name, res, xl.Attribute("data-req")?.Value);
                tasks.Add(res.SyncElement(outDir));
                fileCount++;

                if (tasks.Count < kMaxConcurrentUpdates)
                    continue;

                // Throttle to kMaxConcurrentUpdates
                await Task.WhenAny(tasks.ToArray());        // Do some of the work in parallel.

                for (int i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    if (task.IsCompleted)
                    {
                        if (task.Result == CdnRet.Updated)
                            downloadCount++;
                        tasks.RemoveAt(i);  // remove the one that is done.
                        i--;
                    }
                }

                Debug.Assert(tasks.Count < kMaxConcurrentUpdates);  // I MUST have removed/completed at least 1
            }

            Debug.Assert(tasks.Count < kMaxConcurrentUpdates);
            await Task.WhenAll(tasks.ToArray());        // do ALL this in parallel.

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
            return res.GetAddr(this.UseCdn, this.UseDev);
        }
    }
}
