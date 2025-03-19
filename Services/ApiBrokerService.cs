using APIBroker.Interfaces;
using APIBroker.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace APIBroker.Services
{
    public class ApiBrokerService : IApiBrokerService
    {
        private readonly IDistributedCache _cache;
        private readonly List<string> _apiClients;
        private readonly TimeSpan _cacheDuration;
        private const string ClientIndexKey = "ApiBroker:ClientIndex";
        private const string FailedClientKey = "ApiBroker:FailedClients";
        private static readonly HttpClient _httpClient = new HttpClient();

        public ApiBrokerService(IDistributedCache cache, IConfiguration configuration)
        {
            _cache = cache;
            _apiClients = configuration.GetSection("ApiClients").Get<List<string>>();
            _cacheDuration = TimeSpan.FromMinutes(int.Parse(configuration["RedisCacheSettings:CacheDurationInMinutes"]));
        }

        public async Task<ApiResponse> GetExternalDataAsync()
        {
            int clientIndex = await GetNextClientIndexAsync();
            var availableClients = await GetAvailableClientsAsync();

            if (availableClients.Count < _apiClients.Count)
                clientIndex = 0;

            for (int i = 0; i < availableClients.Count; i++)
            {
                var clientUrl = availableClients[clientIndex];

                try
                {
                    var result = await FetchDataFromClientAsync(clientUrl);

                    if (result != null)
                    {
                        await CacheClientIndexAsync(clientIndex);  // Cache the successful client index
                        return result;
                    }
                }
                catch (Exception)
                {
                    await MarkClientAsFailedAsync(clientUrl);
                }

                // Rotate to the next client in case of failure
                clientIndex = (clientIndex + 1) % availableClients.Count;
            }

            throw new Exception("All clients failed or rate-limited.");
        }

        private async Task<ApiResponse> FetchDataFromClientAsync(string clientUrl)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            int retryCount = 0;
            const int maxRetries = 3;
            const int delayMilliseconds = 2000; // 2 seconds between retries

            while (retryCount < maxRetries)
            {
                try
                {
                    response = await _httpClient.GetAsync(clientUrl);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<ApiResponse>(content);
                        Console.WriteLine($"ID: {result.Id}, Endpoint: {result.Endpoint}");
                        return result;
                    }
                    else
                    {
                        throw new Exception($"{clientUrl} returned status code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Console.WriteLine($"Attempt {retryCount}: {ex.Message}");

                    if (response != null && response.StatusCode == (HttpStatusCode)429) // Rate limit exceeded
                    {
                        throw new Exception("Rate limit exceeded. No retry.");
                    }
                    else if (retryCount < maxRetries)
                    {
                        await Task.Delay(delayMilliseconds); // Wait before retrying
                    }
                    else
                    {
                        throw new Exception("Max retries reached.");
                    }
                }
                finally
                {
                    response?.Dispose();
                }
            }

            throw new Exception("Unexpected error occurred.");
        }

        private async Task<int> GetNextClientIndexAsync()
        {
            var cachedIndex = await _cache.GetStringAsync(ClientIndexKey);
            return int.TryParse(cachedIndex, out int index) ? index % _apiClients.Count : 0;
        }

        private async Task CacheClientIndexAsync(int index)
        {
            await _cache.SetStringAsync(ClientIndexKey, index.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheDuration
            });
        }

        private async Task<List<string>> GetAvailableClientsAsync()
        {
            var failedClientsJson = await _cache.GetStringAsync(FailedClientKey);
            var failedClients = string.IsNullOrEmpty(failedClientsJson)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(failedClientsJson);

            return _apiClients.Except(failedClients).ToList();
        }

        private async Task MarkClientAsFailedAsync(string clientUrl)
        {
            var failedClientsJson = await _cache.GetStringAsync(FailedClientKey);
            var failedClients = string.IsNullOrEmpty(failedClientsJson)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(failedClientsJson);

            if (!failedClients.Contains(clientUrl))
            {
                failedClients.Add(clientUrl);

                await _cache.SetStringAsync(FailedClientKey, JsonConvert.SerializeObject(failedClients),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _cacheDuration
                    });
            }
        }
    }

}
