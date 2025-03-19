namespace APIBroker.Models
{
    public class ProviderMetrics
    {
        public string Provider { get; set; }
        public int TotalRequests { get; set; }
        public int FailureCount { get; set; }
        public int SuccessCount { get; set; }
        public double AverageResponseTime { get; set; }
    }
}
