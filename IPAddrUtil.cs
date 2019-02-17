﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DotStd
{
    public class IPAddrUtil
    {
        // Helper for IPAddress functions.

        // To check if you're connected or not:
        // System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

        public static string kLocalHost = "127.0.0.1";  // System.Net.IPAddress.Parse()

        public static uint ToUInt(IPAddress addr)
        {
            // Get ip4 as 32 bit uint in proper host order.

            ValidState.ThrowIf(addr.AddressFamily != AddressFamily.InterNetwork);   // assume ip4

            var p = addr.GetAddressBytes();
            uint ip = ((uint)p[0]) << 24;
            ip += ((uint)p[1]) << 16;
            ip += ((uint)p[2]) << 8;
            ip += p[3];
            return ip;
        }

        public static ulong ToULong(IPAddress addr, bool high)
        {
            // ip6 is 2 64 bit parts.

            var p = addr.GetAddressBytes();
            int i = high ? 0 : 8;
            ulong ip = ((ulong)p[i + 0]) << 56;
            ip += ((ulong)p[i + 1]) << 48;
            ip += ((ulong)p[i + 2]) << 40;
            ip += ((ulong)p[i + 3]) << 32;
            ip += ((uint)p[i + 4]) << 24;
            ip += ((uint)p[i + 5]) << 16;
            ip += ((uint)p[i + 6]) << 8;
            ip += p[i + 7];
            return 0;
        }

        public static bool IsIP4Private(IPAddress addr)
        {
            // A private ip range. not external.

            if (addr.AddressFamily != AddressFamily.InterNetwork)
                return false;

            byte[] ip = addr.GetAddressBytes();
            if (ip[0] == 10 ||
                (ip[0] == 192 && ip[1] == 168) ||
                (ip[0] == 172 && (ip[1] >= 16 && ip[1] <= 31)))
            {
                return true;
            }
            return false;
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

            // Try to weed out any VistualBox or VPN adapters.
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address;
                }
            }
            catch
            {
                return IPAddress.Parse(kLocalHost); // must return something.
            }
        }

        public static IPAddress FindIPAddrExternal()
        {
            // Use an external service to find my public IP address.
            // only works if: System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

            // https://stackoverflow.com/questions/3253701/get-public-external-ip-address
            // http://bot.whatismyipaddress.com
            // https://ipinfo.io/ip

            // string externalip = new WebClient().DownloadString("http://icanhazip.com");
            string externalip = new WebClient().DownloadString("https://ipinfo.io/ip");

            externalip = externalip.Trim();
            IPAddress addr;
            if (!IPAddress.TryParse(externalip, out addr))
            {
                return null;
            }

            if (IsIP4Private(addr))
            {
                // this is wrong!
            }

            return addr;
        }
    }
}