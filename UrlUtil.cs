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

    public static class UrlUtil
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

        public static string GetProtocol(string url)
        {
            if (url.StartsWith(kHttps))
                return kHttps;
            if (url.StartsWith(kHttp))
                return kHttp;
            return null;
        }

        public static bool IsHttpX(string url)
        {
            // is Http* Scheme ?
            return GetProtocol(null) != null;
        }
        public static bool IsHttpSecure(string url)
        {
            // is Https Scheme ?
            return url.StartsWith(kHttps);
        }
        public static bool IsLocalAddr(string url)
        {
            // Local or external link ?
            if (url == null || url.Length < 2)
                return true;
            return url[0] == '/' && url[1] != '/';
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
            if (!url.EndsWith(kSep) && !url.Contains(kArg))    // not sure why i have to do this.
                url += kSep;
            return url;
        }

        static readonly Lazy<Regex> _regexURL = new Lazy<Regex>(() => new Regex(@"^http(s)?://([\w-]+.)+[\w-]+(/[\w- ./?%&=])?$"));

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
            // Extract just the filename from the URL. No dir, domain name, Clip args after '?' or '#'
            if (url == null)
                return null;

            int i = url.IndexOf(kArg);  // '#' // chop off args.
            if (i >= 0)
            {
                url = url.Substring(0, i);
            }
            i = url.LastIndexOf(kSep);
            if (i >= 0)
            {
                url = url.Substring(i + 1);
            }
            return url;
        }

        public static string ReplaceFile(string url, string nameNew)
        {
            // Replace the file name in the url.
            // Get the dir for the URL.
            string nameOld = GetFileName(url);
            string dir = url.Substring(0, url.Length - nameOld.Length);
            return dir + nameNew;
        }

        public static string GetHostName(string url)
        {
            // Get the host name for the URL.
            // strip http:// and get "addr:port". strip "/dir?args"

            string proto = GetProtocol(url);
            if (proto != null)
                url = url.Substring(proto.Length);
            int i = url.IndexOf(kSep);
            if (i >= 0)
                url = url.Substring(0, i);
            i = url.IndexOf(kArg);
            if (i >= 0)
                url = url.Substring(0, i);
            return url;
        }

        public static string MakeQ(string url, params string[] args)
        {
            // build a local URL link with "Query" args. sPage can be empty.
            // ASSUME Args are already properly encoded! System.Net.WebUtility.UrlEncode() already called.
            // FormUrlEncodedContent already called.

            if (url == null)
                url = "";
            int i = 0;
            foreach (string x in args)
            {
                if (i == 0)
                {
                    url += kArg;
                }
                else
                {
                    url += kArgSep; // arg usually in the form "X=Y"
                }
                url += x;
                i++;
            }
            return url;
        }

        public static string MakeQ2(string url, params string[] args)
        {
            // build a local encoded URL link with paired "Query" args. sPage can be empty.
            if (url == null)
                url = "";
            string sep = kArg;
            for (int i = 0; i < args.Length; i += 2)
            {
                if (string.IsNullOrWhiteSpace(args[i + 1]))
                    continue;
                url += sep;
                url += args[i] + "=" + WebUtility.UrlEncode(args[i + 1]);
                sep = kArgSep;
            }
            return url;
        }
    }
}
