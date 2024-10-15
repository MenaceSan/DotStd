using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Get an IP address range in CIDR format.
    /// e.g. s = "2001:200::/37"
    /// https://en.wikipedia.org/wiki/Classless_Inter-Domain_Routing
    /// https://networkengineering.stackexchange.com/questions/3697/the-slash-after-an-ip-address-cidr-notation
    /// </summary>
    public class IpAddrCidr
    {
        public IPAddress? Addr;
        public int Bits;            // PrefixBits. High to low bits in a mask.

        /// <summary>
        /// Get Size of the range. 32 bits.
        /// </summary>
        public uint GetSize4()
        {
            return (uint)((1 << (32 - Bits)) - 1);
        }
        /// <summary>
        /// Get Size of the range. low 64 bits of 128.
        /// Bits are high to low.
        /// </summary>
        public ulong GetSize6L()
        {
            if (Bits < 64)
                return 0;
            return (ulong)((1ul << (128 - Bits)) - 1);
        }

        /// <summary>
        /// Get Size of the range. high 64 bits of 128.
        /// Bits are high to low.
        /// </summary>
        /// <returns></returns>
        public ulong GetSize6H()
        {
            if (Bits >= 64)
                return 0;
            return (ulong)((1ul << (64 - Bits)) - 1);
        }

        /// <summary>
        /// Set an IPAddress range in string CIDR format.
        /// e.g. s = "2001:200::/37"
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        [MemberNotNullWhen(returnValue: true, member: nameof(Addr))]
        public bool ParseCidr(string s)
        {
            string[] addrA = s.Split('/');
            if (addrA.Length != 2 || !int.TryParse(addrA[1], out int prefixBits))
            {
                return false;
            }
            Bits = prefixBits;

            if (!IPAddress.TryParse(addrA[0], out IPAddress? addr2))
            {
                return false;
            }

            Addr = addr2;
            return true;
        }
    }

    /// <summary>
    /// Helper for IPAddress functions.
    /// To check if you're connected or not:
    /// System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
    /// </summary>
    public static class IPAddrUtil  // : ExternalService
    {
        public const string kLocalHost = "127.0.0.1";  // IPAddress.Loopback = System.Net.IPAddress.Parse(kLocalHost) for ip4. 
        public const int kMaxLen = 64;   // max reasonable length of string.

        /// <summary>
        /// Get ip4 as 32 bit uint in proper host order. from network order.
        /// </summary>
        public static uint ToUInt(IPAddress addr)
        {
            ValidState.ThrowIf(addr.AddressFamily != AddressFamily.InterNetwork);   // assume ip4 || IsIPv4MappedToIPv6
            return ByteUtil.ToUIntN(addr.GetAddressBytes(), 0);
        }

        /// <summary>
        /// Rebuild AddressFamily.InterNetwork address.
        /// Convert n to network order.
        /// </summary>
        public static IPAddress GetIPAddress(uint n)
        {
            uint val = (uint)IPAddress.HostToNetworkOrder((int)n);
            return new IPAddress(val);
        }

        /// <summary>
        /// Get a 64 bit part of IPAddress. may not be all of it.
        /// ip6 is 2 64 bit parts. high and low. from network order to host order
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="high"></param>
        /// <returns></returns>
        public static ulong ToULong(IPAddress addr, bool high)
        {
            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                // IsIPv4MappedToIPv6
                // For example if your IPv4 IP is 209.173.53.167 the valid IPv6 version will be 0:0:0:0:0:ffff:d1ad:35a7
                if (high)
                    return 0;
                return (0xfffful << 32) | ToUInt(addr);      // map ip4 into ip6
            }
            else
            {
                ValidState.ThrowIf(addr.AddressFamily != AddressFamily.InterNetworkV6);   // assume ip6
                return ByteUtil.ToULongN(addr.GetAddressBytes(), high ? 0 : 8);
            }
        }

        /// <summary>
        /// Rebuild AddressFamily.InterNetworkV6 address.
        /// </summary>
        public static IPAddress GetIPAddress(ulong iph, ulong ipl)
        {
            byte[] p = new byte[16];
            ByteUtil.PackULongN(p, 0, iph);
            ByteUtil.PackULongN(p, 8, ipl);
            return new IPAddress(p);
        }

        /// <summary>
        /// Get a single 64 bit hash for the address.
        /// </summary>
        public static long GetHashLong(IPAddress addr)
        {
            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                return ToUInt(addr);    // add offset to get ip6 non collision.
            }
            else if (addr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                ulong u = ToULong(addr, true) ^ ToULong(addr, false);   // combine high and low into a single uint64
                return (long)u;
            }
            // get binary???
            // byte[] p = HashUtil.MergeHash( addr.GetAddressBytes());
            ValidState.ThrowIf(true);
            return 0;
        }

        /// <summary>
        /// Get an IPAddress from string.
        /// </summary>
        /// <param name="hostNameOrAddr"></param>
        /// <returns></returns>
        public static async Task<(IPAddress?, IPHostEntry?)> GetIPAddressAsync(string hostNameOrAddr)
        {
            // Make IPAddress from IP string.
            if (IPAddress.TryParse(hostNameOrAddr, out IPAddress? ipAddr))
            {
                return (ipAddr, null);
            }
            // is it a host name ? raw IP can have odd remapping etc happen here.
            try
            {
                // Dns.GetHostAddressesAsync()
                var hostEntry = await Dns.GetHostEntryAsync(hostNameOrAddr);
                if (hostEntry != null && hostEntry.AddressList.Length > 0)
                {
                    // can resolve to multiple IP's
                    return (hostEntry.AddressList[0], hostEntry);
                }
            }
            catch
            {
                // Ignore throw errors here.
            }
            return (null, null);
        }

        /// <summary>
        /// Is a private ip/local range. not external. similar to IPAddress.IsLoopback(addr)
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static bool IsPrivate(IPAddress? addr)
        {
            if (addr == null) return true;
            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] ip = addr.GetAddressBytes();
                if (ip[0] == 10 ||
                    (ip[0] == 192 && ip[1] == 168) ||
                    (ip[0] == 172 && (ip[1] >= 16 && ip[1] <= 31)))
                    return true;
            }
            else if (addr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (addr.IsIPv6SiteLocal)
                    return true;
            }
            return IPAddress.IsLoopback(addr);
        }

        /// <summary>
        /// Get my local LAN address.
        /// https://stackoverflow.com/questions/6803073/get-local-ip-address
        /// like Dns.GetHostEntry(Dns.GetHostName()) but better.
        /// Try to weed out any VirtualBox or VPN adapters.
        /// </summary>
        /// <returns></returns>
        public static IPAddress? FindLocalIPAddr()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);   // Use Google DNS.
                IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address;
            }
            catch (Exception ex)
            {
                LoggerUtil.DebugError("FindLocalIPAddr", ex);
                return IPAddress.Parse(kLocalHost); // must return something.
            }
        }

        /// <summary>
        /// Use an external service to find my public IP address. May throw.
        /// only works if: System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
        /// https://stackoverflow.com/questions/3253701/get-public-external-ip-address
        /// </summary>
        /// <param name="token">CancellationToken</param>
        /// <returns>ip address as string</returns>
        public static async Task<string?> FindExternalIPAddrStr(CancellationToken token)
        {
            try
            {
                using var client = new HttpClient();
                // string url = "http://bot.whatismyipaddress.com"; // Hangs ??
                // string url = "http://icanhazip.com";  // Hangs ??
                const string kUrl = "https://ipinfo.io/ip";
                HttpResponseMessage response = await client.GetAsync(kUrl, token);
                response.EnsureSuccessStatusCode();
                return (await response.Content.ReadAsStringAsync()).Trim();
            }
            catch (Exception ex)
            {
                LoggerUtil.DebugError("FindIPAddrExternal", ex);
                return null;
            }
        }

        public static async Task<IPAddress?> FindExternalIPAddr(CancellationToken token)
        {
            string? externalip = await FindExternalIPAddrStr(token);
            if (!IPAddress.TryParse(externalip, out IPAddress? addr))
            {
                return null;
            }
            if (IsPrivate(addr))
            {
                // this is wrong!
            }
            return addr;
        }
    }
}
