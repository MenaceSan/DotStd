using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotStd
{
    public class GeoWeatherService : GeoLocation
    {
        // Weather at a location (or near it)
        // "Apps:OpenWeatherMap"

        public async Task<string> GetWeatherJson(string apiKey)
        {
            // Get JSON blob for the weather at some Location.

            // string url = string.Format("http://api.openweathermap.org/data/2.5/forecast/daily?q={0}&units=metric&cnt=1&APPID={1}", txtCity.Text.Trim(), appId);

            using (var wc = new WebClient())
            {
                string url1 = $"http://api.openweathermap.org/data/2.5/weather?lat={this.Latitude}&lon={this.Longitude}&mode=json&units=imperial&APPID={apiKey}";
                string ret = await wc.DownloadStringTaskAsync(url1);
                return ret;
            }
        }
    }
}
