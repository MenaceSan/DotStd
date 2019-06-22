using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DotStd
{
    public class IpAddrCidr
    {
        // Get an address range in CIDR format.
        // e.g. s = "2001:200::/37"
        // https://en.wikipedia.org/wiki/Classless_Inter-Domain_Routing
        // https://networkengineering.stackexchange.com/questions/3697/the-slash-after-an-ip-address-cidr-notation

        public IPAddress Addr;
        public int Bits;            // PrefixBits. High to low bits in a mask.

        public uint GetSize4()
        {
            // Size of the range. 32 bits.
            return (uint)((1 << (32 - Bits)) - 1);
        }
        public ulong GetSize6L()
        {
            // Size of the range. low 64 bits of 128.
            // Bits are high to low.
            if (Bits < 64)
                return 0;
            return (ulong)((1ul << (128 - Bits)) - 1);
        }
        public ulong GetSize6H()
        {
            // Size of the range. high 64 bits of 128.
            // Bits are high to low.
            if (Bits >= 64)
                return 0;
            return (ulong)((1ul << (64 - Bits)) - 1);
        }

        public bool ParseCidr(string s)
        {
            // Get an IPAddress range in string CIDR format.
            // e.g. s = "2001:200::/37"

            string[] addrA = s.Split('/');
            int prefixBits;
            if (addrA.Length != 2 || !int.TryParse(addrA[1], out prefixBits))
            {
                return false;
            }
            Bits = prefixBits;

            IPAddress addr2;
            if (!IPAddress.TryParse(addrA[0], out addr2))
            {
                return false;
            }

            Addr = addr2;
            return true;
        }
    }

    public class IPAddrUtil
    {
        // Helper for IPAddress functions.

        // To check if you're connected or not:
        // System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

        public static string kLocalHost = "127.0.0.1";  // IPAddress.Loopback = System.Net.IPAddress.Parse(kLocalHost) for ip4. 

        public static uint ToUInt(IPAddress addr)
        {
            // Get ip4 as 32 bit uint in proper host order. from network order.
            ValidState.ThrowIf(addr.AddressFamily != AddressFamily.InterNetwork);   // assume ip4 || IsIPv4MappedToIPv6
            return ByteUtil.ToUIntN(addr.GetAddressBytes(), 0);
        }

        public static IPAddress GetIPAddress(uint n)
        {
            // Rebuild AddressFamily.InterNetwork address.
            // Convert n to network order.
            uint val = (uint)IPAddress.HostToNetworkOrder((int)n);
            return new IPAddress(val);
        }

        public static ulong ToULong(IPAddress addr, bool high)
        {
            // ip6 is 2 64 bit parts. high and low. from network order to host order

            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                // IsIPv4MappedToIPv6
                // For example if your IPv4 IP is 209.173.53.167 the valid IPv6 version will be 0:0:0:0:0:ffff:d1ad:35a7
                if (high)
                    return 0;
                return (0xfffful << 32) | ByteUtil.ToUIntN(addr.GetAddressBytes(), 0);
            }
            else
            {
                ValidState.ThrowIf(addr.AddressFamily != AddressFamily.InterNetworkV6);   // assume ip6
                return ByteUtil.ToULongN(addr.GetAddressBytes(), high ? 0 : 8);
            }
        }

        public static IPAddress GetIPAddress(ulong iph, ulong ipl)
        {
            // Rebuild AddressFamily.InterNetworkV6 address.
            byte[] p = new byte[16];
            ByteUtil.PackULongN(p, 0, iph);
            ByteUtil.PackULongN(p, 8, ipl);
            return new IPAddress(p);
        }

        public static long GetHashLong(IPAddress addr)
        {
            // Get a single 64 bit hash for the address.

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

        public static bool IsPrivate(IPAddress addr)
        {
            // A private ip/local range. not external. similar to IPAddress.IsLoopback(addr)

            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] ip = addr.GetAddressBytes();
                if (ip[0] == 10 ||
                    (ip[0] == 192 && ip[1] == 168) ||
                    (ip[0] == 172 && (ip[1] >= 16 && ip[1] <= 31)))
                {
                    return true;
                }
            }
            else if (addr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (addr.IsIPv6SiteLocal)
                    return true;
            }

            return IPAddress.IsLoopback(addr);
        }

        public static IPAddress FindIPAddrLocal()
        {
            // Get my local address.
            // https://stackoverflow.com/questions/6803073/get-local-ip-address

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            // var host = Dns.GetHostEntry(Dns.GetHostName());

            // Try to weed out any VirtualBox or VPN adapters.
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address;
                }
            }
            catch (Exception ex)
            {
                LoggerBase.DebugException("FindIPAddrLocal", ex);
                return IPAddress.Parse(kLocalHost); // must return something.
            }
        }

        public static async Task<string> FindIPAddrExternal2()
        {
            // Use an external service to find my public IP address. May throw.
            // only works if: System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            // https://stackoverflow.com/questions/3253701/get-public-external-ip-address
            try
            {
                using (var wc = new WebClient())
                {
                    // externalip = wc.DownloadString("http://bot.whatismyipaddress.com"); // Hangs ??
                    // externalip = wc.DownloadString("http://icanhazip.com");  // Hangs ??
                    string externalip = await wc.DownloadStringTaskAsync("https://ipinfo.io/ip");
                    return externalip.Trim();
                }
            }
            catch (Exception ex)
            {
                LoggerBase.DebugException("FindIPAddrExternal", ex);
                return null;
            }
        }

        public static async Task<IPAddress> FindIPAddrExternal()
        {
            string externalip = await FindIPAddrExternal2();
            IPAddress addr;
            if (!IPAddress.TryParse(externalip, out addr))
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
