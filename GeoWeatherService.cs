using System.Net.Http;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Get Weather at a location (or near it)
    /// ConfigInfoBase.kApps + "OpenWeatherMap"
    /// </summary>
    public class GeoWeatherService : ExternalService
    {
        string ApiKey;

        public override string Name => "Open Weather";
        public override string BaseURL => "http://api.openweathermap.org/data/2.5/weather";
        public override string Icon => "<i class='fas fa-sync-alt'></i>";

        public GeoWeatherService(string apiKey)
        {
            ApiKey = apiKey;
        }

        /// <summary>
        /// Get a JSON blob for the weather near some Location.
        /// 401 = unauthorized. https://home.openweathermap.org/api_keys
        /// </summary>
        /// <param name="loc"></param>
        /// <returns></returns>
        public async Task<string> GetWeatherJson(GeoLocation loc)
        {
            UpdateTry();
            using (var client = new HttpClient())
            {
                string url1 = $"{BaseURL}?mode=json&units=imperial&lat={loc.Latitude}&lon={loc.Longitude}&APPID={ApiKey}";
                string ret = await client.GetStringAsync(url1);
                return ret;
            }
        }
    }
}
