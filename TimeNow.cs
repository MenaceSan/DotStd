using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Get current most accurate UTC time from some external server on the Internet that i trust.
    /// https://stackoverflow.com/questions/6435099/how-to-get-datetime-from-the-internet
    /// </summary>
    public static class TimeNow // : ExternalService
    {
        /// <summary>
        /// Calculate how far off my local clock is compared to some external trusted source.
        /// </summary>
        public static TimeSpan UtcSkew;

        /// Round time to multiples of n second chunks
        public static DateTime RoundTime(DateTime t, int seconds)
        {
            return new DateTime(Converter.RoundTo(t.Ticks, seconds * TimeSpan.TicksPerSecond), t.Kind);
        }

        /// <summary>
        /// Get the current UTC time with UtcSkew adjustment. 
        /// Allow the system clock (DateTime.UtcNow) to be adjusted to get more accurate (external) UTC real time.
        /// use this instead of DateTime.UtcNow. ONLY USE DateTime.Now for relative time.
        /// </summary>
        public static DateTime Utc
        {
            get
            {
                var t = DateTime.UtcNow;
                return t; // + UtcSkew
            }
        }

        /// <summary>
        /// Get UTC Time from NIST port 13.
        /// May throw System.IO.IOException
        /// </summary>
        /// <returns></returns>
        public static async Task<DateTime> GetUtcNistAsync()
        {
            using var client = new TcpClient();
            DateTime t1 = DateTime.Now; // for data travel time.
            await client.ConnectAsync("time.nist.gov", 13);
            using var streamReader = new StreamReader(client.GetStream());
            string response = await streamReader.ReadToEndAsync();      // "\n60049 23-04-15 19:07:25 50 0 0 360.6 UTC(NIST) *"
            if (response.Length < 25)
                throw new InvalidDataException();   // NIST gave us junk.
            string utcDateTimeString = response.Substring(7, 17);       // get DT part. like: "20-03-04 15:46:52"
            return DateTime.ParseExact(utcDateTimeString, "yy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        /// <summary>
        /// Get the UTC time stamp from a reliable/trusted public HTML server.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<DateTimeOffset?> GetUtcHtmlAsync(string? url)
        {
            using var client = new HttpClient();
            if (url == null)
                url = "https://google.com";

            // DateTime t1 = DateTime.Now; // for data travel time.
            HttpResponseMessage result = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            // DateTime t2 = DateTime.Now;  // how old is the result i got ? data travel time.

            return result.Headers.Date; // Get DateTimeOffset from header.
        }

        /// <summary>
        /// get external UTC time. We can then calculate UtcSkew against local clock.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<DateTime> GetUtcSafeAsync(string? url = null)
        {
            if (url == "")
            {
                try
                {
                    return await GetUtcNistAsync();  // MAY throw. Ignore it.
                }
                catch
                {
                    // NIST is not reliable. Fall through. try another.
                    url = null;
                }
            }
            try
            {
                var dto = await GetUtcHtmlAsync(url);
                if (dto != null)
                {
                    return dto.Value.UtcDateTime;
                }
            }
            catch (Exception ex)
            {
                // FAILED!
                LoggerUtil.DebugError("GetUtcAsync", ex);
            }
            return DateTime.MinValue;
        }

        public static async Task UpdateUtcSkew(string? url = null)
        {
            var utcLocal1 = DateTime.UtcNow;
            var utcPublic = await GetUtcSafeAsync(url);
            var utcLocal2 = DateTime.UtcNow;

            TimeSpan dif1 = (utcLocal2 - utcLocal1) / 2;    // the time in flight.
            utcPublic += dif1;
            UtcSkew = utcPublic - utcLocal2;    // should be 0. else add this to local time to get external public time.
        }
    }
}
