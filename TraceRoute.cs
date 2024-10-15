using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotStd
{
    public static class TraceRoute
    {
        static readonly string kDataStr = "DotStd.TraceRoute";
        static readonly byte[] kBuffer = Encoding.ASCII.GetBytes(kDataStr);

        const int kMaxHops = 20;        // dynamic adjust this? Windows does 30.
        const int kRequestTimeout = 4000;   // dynamic adjust this?

        /// <summary>
        /// Trace ping results for a single node.
        /// </summary>
        public class Node
        {
            public readonly int _Ttl;    // count from 1
            public IPStatus _Status = IPStatus.Unknown; // Success = 0
            public IPAddress _ReplyAddress = IPAddress.None;
            public long _ElapsedMilliseconds;

            public string? _DnsHostName = ""; // optional reverse DNS lookup 

            public bool IsComplete => _Status == IPStatus.Success;

            public bool IsUseful => _Status != IPStatus.TimedOut && _Status != IPStatus.Unknown;

            public bool IsValidReplyAddress
            {
                // the _ReplyAddress looks like it might be valid ? filter known bad addresses.
                get
                {
                    if (_ReplyAddress == IPAddress.None || _ReplyAddress == IPAddress.IPv6None || _ReplyAddress == IPAddress.Any)
                        return false;
                    if (_ReplyAddress.AddressFamily <= System.Net.Sockets.AddressFamily.Unspecified)
                        return false;
                    if (_ReplyAddress.GetAddressBytes().All(b => b == 0)) // InterNetwork and 0
                        return false;
                    if (IPAddress.IsLoopback(_ReplyAddress))    // not a real address.
                        return false;
                    return true;
                }
            }

            public Node(int ttl)
            {
                _Ttl = ttl;
            }

            public async Task UpdateDnsHostName(CancellationToken cancel)
            {
                if (cancel.IsCancellationRequested)
                    return;
                if (!IsValidReplyAddress)
                    return;
                try
                {
                    IPHostEntry hostEntry = (cancel == CancellationToken.None) ?
                        await Dns.GetHostEntryAsync(_ReplyAddress) :
                        await Dns.GetHostEntryAsync(_ReplyAddress.ToString(), _ReplyAddress.AddressFamily, cancel);
                    _DnsHostName = hostEntry.HostName;
                }
                catch
                {
                    // just eat failures/cancel. System.Net.Sockets.SocketException. 'No such host is known.'
                }
            }

            public async Task SendPingAsync(IPAddress ipAddr, byte[] buffer, CancellationTokenSource cancel)
            {
                using (var pingSender = new Ping())     // not shareable between tasks.
                {
                    var pingOptions = new PingOptions
                    {
                        DontFragment = true,
                        Ttl = _Ttl,
                    };
                    var stopWatch = new Stopwatch();

                    stopWatch.Start();
                    // note: Ping has no Cancel token usage.
                    PingReply pingReply = await pingSender.SendPingAsync(
                        ipAddr,
                        kRequestTimeout,
                        buffer,
                        pingOptions);
                    stopWatch.Stop();

                    _ElapsedMilliseconds = stopWatch.ElapsedMilliseconds;
                    _Status = pingReply.Status;
                    _ReplyAddress = pingReply.Address;
                }

                await UpdateDnsHostName(cancel.Token);

                if (IsComplete && !cancel.IsCancellationRequested)
                {
                    // trace complete. ignore/cancel the rest.
                    // Note: all ttl after the last look the same. we need to merge the last node?
                    // if time is too small give the other tasks a little more time ?
                    try
                    {
                        await Task.Delay((_ElapsedMilliseconds < kRequestTimeout / 2) ? 500 : 100, cancel.Token);
                        cancel.Cancel();
                    }
                    catch (System.Threading.Tasks.TaskCanceledException)
                    {
                        // specific ignore.
                    }
                }
            }

            public virtual string GetStr()
            {
                var sb = new StringBuilder();
                sb.Append(_Ttl.ToString(System.Globalization.CultureInfo.InvariantCulture).PadRight(4, ' '));

                string pingReplyAddress;
                string strElapsedMilliseconds = "";

                switch (_Status)
                {
                    case IPStatus.Unknown:
                        pingReplyAddress = "Could not get result";
                        strElapsedMilliseconds = "*";
                        break;
                    case IPStatus.TimedOut:
                        pingReplyAddress = "Request timed out.";
                        strElapsedMilliseconds = "*";
                        break;
                    case IPStatus.Success:
                    case IPStatus.TtlExpired:
                        pingReplyAddress = _ReplyAddress.ToString();
                        break;
                    default:
                        pingReplyAddress = IsValidReplyAddress ? _ReplyAddress.ToString() : "*";
                        strElapsedMilliseconds = _Status.ToString();
                        break;
                }

                if (string.IsNullOrEmpty(strElapsedMilliseconds))
                {
                    strElapsedMilliseconds = _ElapsedMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    strElapsedMilliseconds = $"{strElapsedMilliseconds} ms";
                }
                sb.Append(strElapsedMilliseconds.PadRight(10, ' '));

                if (!string.IsNullOrEmpty(_DnsHostName))
                {
                    pingReplyAddress += $" ({_DnsHostName})";
                }
                sb.Append(pingReplyAddress);
                return sb.ToString();
            }
        }

        public static async Task<Node[]> GetResultsAsync(IPAddress ipAddr)
        {
            Contract.EndContractBlock();

            var aNodes = new Node[kMaxHops];
            var aTasks = new Task[kMaxHops];    // dispatch parallel tasks for each hop/node
            var cancelSource = new CancellationTokenSource();

            for (int hop = 0; hop < kMaxHops; hop++)
            {
                var node = new Node(hop + 1);
                aNodes[hop] = node;
                aTasks[hop] = node.SendPingAsync(ipAddr, kBuffer, cancelSource);
            }

            var tcs = new TaskCompletionSource();
            cancelSource.Token.Register(() => tcs.TrySetCanceled(), false);
            await Task.WhenAny(Task.WhenAll(aTasks), tcs.Task);     // wait for them all to finish (or be canceled)

            for (int hop = 0; hop < kMaxHops; hop++)
            {
                // var task = aTasks[hop];
                // (traceTask.Status == TaskStatus.RanToCompletion) // good
                // TaskStatus.Cancel = OperationCanceledException // canceled.

                var node = aNodes[hop];
                if (node.IsComplete)
                {
                    // the end. merge any extra data we got back to the last entry?
                    Array.Resize(ref aNodes, hop + 1);
                    return aNodes;
                }
            }

            int hopLast = aNodes.Length - 1;
            for (; hopLast > 0; hopLast--)
                if (aNodes[hopLast].IsUseful)
                    break;
            hopLast += 2;
            if (hopLast < aNodes.Length)
                Array.Resize(ref aNodes, hopLast);    // Trim to last useful node.
            return aNodes;
        }

        public static async Task PrintRouteAsync(IPAddress ipAddr, StreamWriter sw)
        {
            Contract.EndContractBlock();
            await sw.WriteLineAsync($"Route to {ipAddr}, {kMaxHops} hops max, {kBuffer.Length} byte packets.");
            await sw.FlushAsync();

            var results = await GetResultsAsync(ipAddr);
            foreach (var res in results)
            {
                await sw.WriteLineAsync(res.GetStr());
            }
        }

        public static async Task PrintRouteAsync(string hostNameOrAddress, StreamWriter sw)
        {
            Contract.EndContractBlock();
            await sw.WriteLineAsync($"Route to '{hostNameOrAddress}'.");
            await sw.FlushAsync();

            (IPAddress? ipAddr, IPHostEntry? hostEntry) = await IPAddrUtil.GetIPAddressAsync(hostNameOrAddress);
            if (ipAddr == null)
            {
                await sw.WriteLineAsync($"'{hostNameOrAddress}' does not resolve to an address");
                return;
            }

            await PrintRouteAsync(ipAddr, sw);
        }

        /// <summary>
        /// Runs trace route and writes result to console.
        /// </summary>
        public static async Task PrintRouteAsync(string hostNameOrAddress)
        {
            Contract.EndContractBlock();
            using (var console = Console.OpenStandardOutput())
            using (var sw = new StreamWriter(console))
            {
                await PrintRouteAsync(hostNameOrAddress, sw);
            }
        }
    }
}
