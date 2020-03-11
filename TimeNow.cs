﻿using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DotStd
{
    public static class TimeNow
    {
        // Get current UTC time from some external server on the Internet that i trust.
        // https://stackoverflow.com/questions/6435099/how-to-get-datetime-from-the-internet

        public static TimeSpan Offset;  // Offset to add to system time to get real time. UTC.

        public static DateTime Utc  // Get the time adjusted to real time. 
        {
            // Allow the system clock (DateTime.UtcNow) to be adjusted to get more accurate UTC time.
            // use this instead of DateTime.UtcNow. ONLY USE DateTime.Now for relative time.
            get
            {
                var t = DateTime.UtcNow;
                return t;
            }
        }

        public static async Task<DateTime> GetNistAsync()
        {
            // Get UTC Time from NIST port 13.
            using (var client = new TcpClient())
            {
                DateTime t1 = DateTime.Now; // for data travel time.
                await client.ConnectAsync("time.nist.gov", 13);
                using (var streamReader = new StreamReader(client.GetStream()))
                {
                    string response = await streamReader.ReadToEndAsync();      // 58912 20-03-04 15:46:52 55 0 0 793.7 UTC(NIST) * 
                    string utcDateTimeString = response.Substring(7, 17);       // 20-03-04 15:46:52
                    return DateTime.ParseExact(utcDateTimeString, "yy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                }
            }
        }

        public static async Task<DateTimeOffset?> GetUrlAsync(string url)
        {
            // Get the UTC time stamp from a reliable public server.

            using (var client = new HttpClient())
            {
                if (url == null)
                    url = "https://google.com";

                DateTime t1 = DateTime.Now; // for data travel time.
                HttpResponseMessage result = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                DateTime t2 = DateTime.Now;  // how old is the result i got ? data travel time.

                return result.Headers.Date;
            }
        }

        public static async Task<DateTime> GetUtcAsync(string url = null)
        {
            // get external UTC time.

            try
            {
                return await GetNistAsync();
            }
            catch
            {
                // Fall through.
            }

            try
            {
                var dto = await GetUrlAsync(url);
                if (dto != null)
                {
                    return dto.Value.UtcDateTime;
                }
            }
            catch
            {
            }

            return DateTime.MinValue;
        }

    }
}
