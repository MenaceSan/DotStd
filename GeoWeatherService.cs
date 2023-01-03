using System.Net.Http;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Get Weather at a location (or near it)
    /// ConfigInfoBase.kApps + "OpenWeatherMap"
    /// </summary>
    public class GeoWeatherService : GeoLocation
    {
        public async Task<string> GetWeatherJson(string apiKey)
        {
            // Get a JSON blob for the weather near some Location.
            // 401 = unauthorized. https://home.openweathermap.org/api_keys
            const string baseUrl = "http://api.openweathermap.org/data/2.5/weather";

            using (var client = new HttpClient())
            {
                string url1 = $"{baseUrl}?mode=json&units=imperial&lat={this.Latitude}&lon={this.Longitude}&APPID={apiKey}";
                string ret = await client.GetStringAsync(url1);
                return ret;
            }
        }
    }
}
