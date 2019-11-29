using System.Net.Http;
using System.Threading.Tasks;

namespace DotStd
{
    public class GeoWeatherService : GeoLocation
    {
        // Weather at a location (or near it)
        // "Apps:OpenWeatherMap"

        public async Task<string> GetWeatherJson(string apiKey)
        {
            // Get a JSON blob for the weather near some Location.
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
