using System.Text.Json.Serialization;

namespace APIBroker.Models
{
    public class FreeIpLocation
    {
        [JsonPropertyName("countryName")]
        public string CountryName { get; set; }

        [JsonPropertyName("cityName")]
        public string CityName { get; set; }
    }
}
