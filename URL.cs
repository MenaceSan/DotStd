using System.Text;
using System.Text.RegularExpressions;

namespace DotStd
{
    public static class URL
    {
        // Helper for URLs always use "/" as path separators.
        // similar to System.Uri or System.UriBuilder
        // in Core use: WebUtility.UrlEncode()

        public const string kHttps = "https://";
        public const string kHttp = "http://";
        public const string kSep = "/";

        public static bool IsHttpX(string sURL)
        {
            // is Http* Scheme ?
            return sURL.StartsWith(kHttps) || sURL.StartsWith(kHttp);
        }
        public static bool IsHttpSecure(string sURL)
        {
            // is Https Scheme ?
            return sURL.StartsWith(kHttps);
        }

        static Regex _regexURL = null;
        public static bool IsValidURL(string url)
        {
            // Stricter version of URL
            if (string.IsNullOrWhiteSpace(url))
                return false;
            if (_regexURL == null)
            {
                _regexURL = new Regex(@"^http(s)?://([\w-]+.)+[\w-]+(/[\w- ./?%&=])?$");
            }
            return _regexURL.IsMatch(url);
        }

        static Regex _regexURL2 = null;
        public static bool IsValidURL2(string url)
        {
            // More forgiving version of URL
            if (string.IsNullOrWhiteSpace(url))
                return false;
            if (_regexURL2 == null)
            {
                _regexURL2 = new Regex(@"^^http(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_=]*)?$");
            }
            return _regexURL2.IsMatch(url);
        }

        public static string GetSubDomain(string reqHost)
        {
            // reqHost = context.Request.Host.ToString(). e.g. "subdom.test.com:234"
            // RETURN null for "test.com" or "localhost:44322"
            // ASSUME not /Path
            if (reqHost == null)
                return null;
            int i = reqHost.IndexOf(kSep);
            if (i >= 0)
            {
                reqHost = reqHost.Substring(0, i);
            }
            i = reqHost.IndexOf('.');
            if (i < 0)      // no dots.
                return null;
            int j = reqHost.IndexOf('.', i + 1);    // MUST have a second dot.
            if (j < 0)
                return null;
            return reqHost.Substring(0, i);
        }

        public static string MakeHttpX(string sURL, bool bSetHttps)
        {
            // Make sure the URL has the proper prefix. http or https
            if (sURL.StartsWith(kHttps))
            {
                if (!bSetHttps)
                    return kHttp + sURL.Substring(8);
            }
            else if (sURL.StartsWith(kHttp))
            {
                if (bSetHttps)
                    return kHttps + sURL.Substring(7);
            }
            else
            {
                return (bSetHttps ? kHttps : kHttp) + sURL;
            }
            return sURL;
        }

        public static string MakeHttpProper(string sURL)
        {
            // Make sure the URL has a prefix. default to https if it does not.
            if (!sURL.StartsWith(kHttp) && !sURL.StartsWith(kHttps))    // make sure it has prefix.
                sURL = kHttps + sURL;
            if (!sURL.EndsWith(kSep) && !sURL.Contains("?"))    // not sure why i have to do this.
                sURL += kSep;
            return sURL;
        }

        public static string Combine(params string[] array)
        {
            // Like Path.Combine but for URLs
            var sb = new StringBuilder();
            int i = 0;
            bool endSep = false;
            foreach (string a in array)
            {
                if (string.IsNullOrWhiteSpace(a))    // doesnt count
                    continue;
                bool startSep = a.StartsWith(kSep);
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

        public static string GetFileName(string sURL)
        {
            // Extract just the filename from the URL. No domain name, Clip args after '?' or '#'
            try
            {
                int i = sURL.LastIndexOf(kSep);
                if (i >= 0)
                {
                    sURL = sURL.Substring(sURL.LastIndexOf(kSep) + 1);
                }
                i = sURL.IndexOf("?");  // '#'
                if (i >= 0)
                {
                    sURL = sURL.Substring(0, i);
                }
                return sURL;
            }
            catch
            {
                return sURL;
            }
        }

        public static string Make(string sPage, params string[] sArgs)
        {
            // build a local URL link with "Query" args. sPage can be empty.
            // ASSUME Args are already properly encoded! System.Net.WebUtility.UrlEncode()

            if (sPage == null)
                sPage = "";
            int i = 0;
            foreach (string x in sArgs)
            {
                if (i == 0)
                {
                    sPage += "?";
                }
                else
                {
                    sPage += "&"; // arg usually in the form "X=Y"
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
            for (int i = 0; i < sArgs.Length; i += 2)
            {
                if (i == 0)
                {
                    sPage += "?";
                }
                else
                {
                    sPage += "&";
                }
                sPage += sArgs[i] + "=" + sArgs[i + 1];
            }
            return sPage;
        }
    }
}
