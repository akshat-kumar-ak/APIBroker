using APIBroker.Models;

namespace APIBroker.Interfaces
{
    public interface IRedisCacheService
    {
        Task TrackProviderMetricsAsync(string provider, bool isSuccess, double responseTime);
        Task<ProviderMetrics> GetProviderMetricsAsync(string provider);
    }
}
