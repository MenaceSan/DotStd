using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DotStd
{
    /// <summary>
    /// Util/Helper for host names.
    /// </summary>
    public class HostNameUtil
    {
        public static bool IsValidHostName([NotNullWhen(true)] string? hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                return false;
            // made of valid chars ?
            return true;
        }

        /// <summary>
        /// Get Subdomain from hostname if it has one.
        /// ASSUME no protocol prefix "http://" etc. ASSUME not /Path\'' 
        /// </summary>
        /// <param name="reqHost">context.Request.Host.ToString().ToLower(). e.g. "subdom.test.com:443" or special "test.localhost:80"</param>
        /// <returns>null for "test.com" or "localhost:44322" (has no subdomain)</returns>
        public static string? GetSubDomain(string? reqHost)
        {
            if (string.IsNullOrWhiteSpace(reqHost))
                return null;
            int i = reqHost.IndexOf(':');  // chop off port.
            if (i >= 0)
            {
                reqHost = reqHost.Substring(0, i);
            }

            i = reqHost.IndexOf('.');
            if (i < 0)      // no dots.
                return null; // no subdomain

            if (!reqHost.EndsWith("localhost"))
            {
                int j = reqHost.IndexOf('.', i + 1);    // MUST have a second dot. subdomain.maindomain.com
                if (j < 0)
                    return null;    // no subdomain
            }

            return reqHost.Substring(0, i);
        }
    }
}
