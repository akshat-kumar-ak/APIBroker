using APIBroker.Models;

namespace APIBroker.Interfaces
{
    public interface IApiBrokerService
    {
        Task<ApiResponse> GetExternalDataAsync();

    }
}
