using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DotStd
{
    public enum DeviceTypeId
    {
        // classify the clients device. is Mobile device type? for Push Notification. for PushSharp
        // used by user_device.DeviceTypeId

        Unknown = 0,

        iOS = 1,            // Apple iOS // APN
        Android = 2,        // Google Android  // FireBase ?
        Amazon = 3,         // Amazon-fireos
        Windows10 = 4,      // UWP apps. Windows 10.
        Mac = 5,            // Xamarin for Mac OS.

        // Blackberry?
        // Assume a laptop/desktop/tablet format?

        MSIE_Old = 15,        // https://stackoverflow.com/questions/10964966/detect-ie-version-prior-to-v9-in-javascript
    }

    public static class URL
    {
        // Helper for URLs always use "/" as path separators.
        // similar to System.Uri or System.UriBuilder
        // in Core use: WebUtility.UrlEncode() (was FormUrlEncodedContent)

        public const string kHttps = "https://";
        public const string kHttp = "http://";
        public const string kSep = "/";
        public const char kSepChar = '/';

        public const string kArg = "?"; // start of args on URL.
        public const string kArgSep = "&";  // sep args on URL.

        // TODO AddReturnUrl and build args.

        public static bool IsHttpX(string url)
        {
            // is Http* Scheme ?
            return url.StartsWith(kHttps) || url.StartsWith(kHttp);
        }
        public static bool IsHttpSecure(string url)
        {
            // is Https Scheme ?
            return url.StartsWith(kHttps);
        }

        static readonly Lazy<Regex> _regexURL = new Lazy<Regex>( () => new Regex(@"^http(s)?://([\w-]+.)+[\w-]+(/[\w- ./?%&=])?$")); 

        public static bool IsValidURL(string url)
        {
            // Stricter version of URL validation
            if (string.IsNullOrWhiteSpace(url))
                return false;
         
            return _regexURL.Value.IsMatch(url);
        }

        static readonly Lazy<Regex> _regexURL2 = new Lazy<Regex>(() => new Regex(@"^^http(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_=]*)?$"));  
 
        public static bool IsValidURL2(string url)
        {
            // More forgiving version of URL
            if (string.IsNullOrWhiteSpace(url))
                return false;
           
            return _regexURL2.Value.IsMatch(url);
        }

        public static string GetSubDomain(string reqHost)
        {
            // reqHost = context.Request.Host.ToString().ToLower(). e.g. "subdom.test.com:443" or special "test.localhost:80"
            // RETURN null for "test.com" or "localhost:44322" (has no subdomain)
            // ASSUME not /Path\'' 
            // ASSUME no prefix "http://" etc.

            if (reqHost == null)
                return null;
            int i = reqHost.IndexOf(kSep);  // chop off extra stuff.
            if (i >= 0)
            {
                reqHost = reqHost.Substring(0, i);
            }
            i = reqHost.IndexOf(':');  // chop off port.
            if (i >= 0)
            {
                reqHost = reqHost.Substring(0, i);
            }

            i = reqHost.IndexOf('.');
            if (i < 0)      // no dots.
                return null;

            if (!reqHost.EndsWith("localhost"))
            {
                int j = reqHost.IndexOf('.', i + 1);    // MUST have a second dot.
                if (j < 0)
                    return null;
            }

            return reqHost.Substring(0, i);
        }

        public static string MakeHttpX(string url, bool bSetHttps)
        {
            // Make sure the URL has the proper prefix. HTTP or HTTPS
            if (url.StartsWith(kHttps))
            {
                if (!bSetHttps)
                    return kHttp + url.Substring(8);
            }
            else if (url.StartsWith(kHttp))
            {
                if (bSetHttps)
                    return kHttps + url.Substring(7);
            }
            else
            {
                return (bSetHttps ? kHttps : kHttp) + url;
            }
            return url;
        }

        public static string MakeHttpProper(string url)
        {
            // Make sure the URL has a prefix. default to HTTPS if it does not.
            if (!url.StartsWith(kHttp) && !url.StartsWith(kHttps))    // make sure it has prefix.
                url = kHttps + url;
            if (!url.EndsWith(kSep) && !url.Contains("?"))    // not sure why i have to do this.
                url += kSep;
            return url;
        }

        public static string Combine(params string[] array)
        {
            // Like Path.Combine() but for URLs. CombineUrl. Ignore nulls.
            // Does not add an end or start /

            var sb = new StringBuilder();
            int i = 0;
            bool endSep = false; // last entry ends with sep?

            foreach (string a in array)
            {
                if (string.IsNullOrWhiteSpace(a))    // doesn't count. skip it.
                    continue;

                bool startSep = a.StartsWith(kSep); // next entry starts with sep?
                if (i > 0 && endSep && startSep)
                {
                    sb.Append(a.Substring(1));
                }
                else if (i > 0 && !endSep && !startSep)
                {
                    sb.Append(kSep);
                    sb.Append(a);
                }
                else
                {
                    sb.Append(a);
                }

                endSep = a.EndsWith(kSep);
                i++;
            }

            return sb.ToString();
        }

        public static string GetFileName(string url)
        {
            // Extract just the filename from the URL. No domain name, Clip args after '?' or '#'
            try
            {
                int i = url.LastIndexOf(kSep);
                if (i >= 0)
                {
                    url = url.Substring(url.LastIndexOf(kSep) + 1);
                }
                i = url.IndexOf("?");  // '#' // chop off args.
                if (i >= 0)
                {
                    url = url.Substring(0, i);
                }
                return url;
            }
            catch
            {
                return url;
            }
        }

        public static string GetDir(string url)
        {
            // Get the dir for the URL.
            string name = GetFileName(url);
            return url.Substring(0, url.Length - name.Length);
        }

        public static string Make(string sPage, params string[] sArgs)
        {
            // build a local URL link with "Query" args. sPage can be empty.
            // ASSUME Args are already properly encoded! System.Net.WebUtility.UrlEncode() already called.
            // FormUrlEncodedContent already called.

            if (sPage == null)
                sPage = "";
            int i = 0;
            foreach (string x in sArgs)
            {
                if (i == 0)
                {
                    sPage += kArg;
                }
                else
                {
                    sPage += kArgSep; // arg usually in the form "X=Y"
                }
                sPage += x;
                i++;
            }
            return sPage;
        }

        public static string Make2(string sPage, params string[] sArgs)
        {
            // build a local URL link with paired "Query" args. sPage can be empty.
            if (sPage == null)
                sPage = "";
            string sep = kArg;
            for (int i = 0; i < sArgs.Length; i += 2)
            {
                if (string.IsNullOrWhiteSpace(sArgs[i + 1]))
                    continue;
                sPage += sep;
                sPage += sArgs[i] + "=" + WebUtility.UrlEncode(sArgs[i + 1]);
                sep = kArgSep;
            }
            return sPage;
        }
    }
}
