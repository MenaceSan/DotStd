using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace DotStd
{
    public static class CdnUtil
    {
        // Pull .js and .css files from a CDN and make local copies for failover and dev purposes.
        // Read a file called 'CdnAll.html' that contains all the links to my CDN files.
        // pull local version of these. to "asp-fallback-src" or "asp-fallback-href"
        // Similar function to Bower but centered on the CDN not the FULL dev packages.
        // TODO read the 'libman.json' file to build this ?! Since libman doesnt contain 'integrity' at this time we can just use libman directly. (2018)

        public const string kAll = "CdnAll.html";
        public const string kDataLibPath = "data-fallback-lib";      // If i pulled the lib from Bower
        public const string kMin = ".min.";
        public const string kMin2 = "-min.";    // alternate style.

        public static string GetPhysPathFromWeb(string w)
        {
            // ~ = wwwroot
            // / = wwwroot
            // NO / = root of app.

            if (w.StartsWith("~/"))
            {
                return "wwwroot" + w.Substring(1);
            }

            return w;
        }

        public static void SyncCdn(string cdnAllFilePath, string outDir)
        {
            // Read the HTML/XML file kAll from Resource.
            // Pull all files from the CDN that we want locally as backups.
            // Write out the local stuff to outDir. e.g. "wwwroot/cdn"
            if (!File.Exists(cdnAllFilePath))
                return;

            XDocument doc = XDocument.Load(cdnAllFilePath);     // Use HTML agility pack to deal with proper encoding.

            // pull all 'link' and 'script' elements
            foreach (XNode node in doc.DescendantNodes())
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                XElement xl = (XElement)node;
                if (xl.Name != "script" && xl.Name != "link")
                    continue;

                string typeExt = (xl.Name == "script") ? "src" : "href";
                XAttribute src = xl.Attribute(typeExt);
                XAttribute integrity = xl.Attribute("integrity");
                XAttribute dst = xl.Attribute("asp-fallback-" + typeExt);
                XAttribute dstDev = xl.Attribute("data-dev-" + typeExt);    // Allow null default.

                // test hash e.g. "sha256-", "sha384-"
                int i = integrity.Value.IndexOf('-');
                if (i <= 0)
                    continue;
                byte[] hashCode1 = Convert.FromBase64String(integrity.Value.Substring(i + 1));
                byte[] hashCode2 = null;
                // string debugHash1 = Convert.ToBase64String(hashCode1);
                // string debugHash2;

                var hasher = new HashUtil(integrity.Value);

                string dstPath = GetPhysPathFromWeb(dst.Value); // Make a real path.
                if (File.Exists(dstPath))
                {
                    // Is current file ok?
                    hashCode2 = hasher.GetHashFile(dstPath);
                    // debugHash2 = Convert.ToBase64String(hashCode2);
                    if (ComparerDef.CompareBytes(hashCode1, hashCode2) == 0)     // match.
                        continue;
                    hasher.Init();
                }

                // Pull the file.
                var dl = new WebDownloader(src.Value, dstPath);
                dl.DownloadFileRaw();  // Assume dir is created on demand.

                // Now test again!
                hashCode2 = hasher.GetHashFile(dstPath);
                // debugHash2 = Convert.ToBase64String(hashCode2);
                if (ComparerDef.CompareBytes(hashCode1, hashCode2) != 0)     // MUST match.
                {
                    throw new Exception("CDN hash does not match for " + dstPath);
                }

                if (src.Value.Contains(kMin) || src.Value.Contains(kMin2))
                {
                    // Pull the non-minified (Dev) version as well.
                    if (dstDev == null || dstDev.Value != dst.Value)
                    {
                        dstPath = (dstDev != null) ? GetPhysPathFromWeb(dstDev.Value) : 
                            dstPath.Replace(kMin, ".").Replace(kMin2, ".").Replace("/min/","/");
                        string srcPath = src.Value.Replace(kMin, ".").Replace(kMin2, ".").Replace("/min/", "/");
                        var dl2 = new WebDownloader(srcPath, dstPath);
                        dl2.DownloadFileRaw();
                    }
                }
            }
        }
    }
}
