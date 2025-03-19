namespace APIBroker.Models
{
    public class ProviderTrackRequest
    {
        public string Provider { get; set; }
        public bool IsSuccess { get; set; }
        public double ResponseTime { get; set; }
    }
}
