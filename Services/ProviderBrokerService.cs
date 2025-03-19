using APIBroker.Interfaces;
using APIBroker.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace APIBroker.Services
{
    public class ProviderBrokerService : IProviderBrokerService
    {
        private readonly Dictionary<string, QualityMetrics> _metrics = new();
        private readonly HttpClient _httpClient = new();
        private readonly IRedisCacheService _redisCacheService;

        public ProviderBrokerService(IRedisCacheService redisCacheService)
        {
            foreach (var provider in new ProviderConfig().Providers)
            {
                _metrics[provider.Name] = new QualityMetrics();
            }

            _redisCacheService = redisCacheService;
        }

        public async Task<IpLocation> GetLocationAsync(string ip)
        {
            List<string> triedProviders = new();

            while (true)
            {
                var provider = await SelectBestProviderAsync(triedProviders);

                if (provider == null) throw new Exception("No available provider after trying all options.");

                var url = $"{provider.BaseUrl}{ip}";
                var startTime = DateTime.UtcNow;

                try
                {
                    var response = await _httpClient.GetAsync(url);
                    var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    if (response.IsSuccessStatusCode)
                    {
                        await _redisCacheService.TrackProviderMetricsAsync(provider.Name, true, responseTime);
                        var jsonString = await response.Content.ReadAsStringAsync();
                        IpLocation ipLocation = new IpLocation { Ip = ip };

                        if (provider.Name == "free_ip_api")
                        {
                            var freeIpLocation = JsonSerializer.Deserialize<FreeIpLocation>(jsonString);
                            ipLocation.City = freeIpLocation.CityName;
                            ipLocation.Country = freeIpLocation.CountryName;
                            ipLocation.Provider = "free_ip_api";
                        }
                        else
                        {
                            ipLocation = JsonSerializer.Deserialize<IpLocation>(jsonString);
                            ipLocation.Provider = provider.Name;
                        }
                        Console.WriteLine($"Provider: {ipLocation.Provider}, Country: {ipLocation.Country}, City: {ipLocation.City}");
                        return ipLocation;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        Console.WriteLine($"Provider {provider.Name} exceeded rate limit. Trying next provider.");
                        await _redisCacheService.TrackProviderMetricsAsync(provider.Name, false, responseTime);
                        triedProviders.Add(provider.Name);
                    }
                    else
                    {
                        await _redisCacheService.TrackProviderMetricsAsync(provider.Name, false, responseTime);
                        throw new Exception($"Provider {provider.Name} returned error: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error with provider {provider.Name}: {ex.Message}");
                    await _redisCacheService.TrackProviderMetricsAsync(provider.Name, false, 1000); // Penalize failure
                    triedProviders.Add(provider.Name);

                    if (triedProviders.Count == new ProviderConfig().Providers.Count)
                    {
                        throw new Exception("All providers failed. No further retries possible.");
                    }
                }
            }
        }


        public async Task<Provider> SelectBestProviderAsync(List<string> excludedProviders)
        {
            var config = new ProviderConfig();
            var tasks = config.Providers
                .Where(p => !excludedProviders.Contains(p.Name))
                .Select(p => _redisCacheService.GetProviderMetricsAsync(p.Name));

            var providerMetrics = await Task.WhenAll(tasks);

            return config.Providers
                .Where(p => !excludedProviders.Contains(p.Name))
                .Join(providerMetrics, p => p.Name, m => m.Provider, (p, m) => new { Provider = p, Metrics = m })
                .OrderBy(x => x.Metrics.AverageResponseTime)
                .ThenBy(x => x.Metrics.FailureCount)
                .FirstOrDefault(x => x.Metrics.TotalRequests < x.Provider.RateLimit)?
                .Provider;
        }
    }

}
