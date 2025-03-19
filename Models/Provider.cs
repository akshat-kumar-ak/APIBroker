namespace APIBroker.Models
{
    public class Provider
    {
        public string Name { get; set; }
        public string BaseUrl { get; set; }
        public int ErrorCount { get; set; }
        public double AverageResponseTime { get; set; }
        public int RequestCount { get; set; }
        public DateTime LastRequestTime { get; set; }
        public int RateLimit { get; set; } = 60; // Example rate limit per minute
    }

    public class QualityMetrics
    {
        public int ErrorCount { get; set; }
        public double AverageResponseTime { get; set; }
        public int RequestsInLastMinute { get; set; }
    }

    public class ProviderConfig
    {
        public List<Provider> Providers { get; set; } = new List<Provider>
        {
            new Provider { Name = "ip_api", BaseUrl = "http://ip-api.com/json/", RateLimit = 45 },
            new Provider { Name = "free_ip_api", BaseUrl = "https://freeipapi.com/api/json/", RateLimit = 60 }
        };
    }

}
