using System.Text.Json.Serialization;

namespace APIBroker.Models
{
    public class IpLocation
    {
        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }
        public string Ip { get; set; }
        public string Provider { get; set; }
    }
}
