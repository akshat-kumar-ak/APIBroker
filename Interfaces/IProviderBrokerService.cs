using APIBroker.Models;

namespace APIBroker.Interfaces
{
    public interface IProviderBrokerService
    {
        Task<IpLocation> GetLocationAsync(string ip);
        Task<Provider> SelectBestProviderAsync(List<string> excludedProviders);
    }
}
