using APIBroker.Interfaces;
using APIBroker.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace APIBroker.Services
{

    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDatabase _database;
        private readonly TimeSpan _timeFrame = TimeSpan.FromMinutes(5);

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }

        public async Task TrackProviderMetricsAsync(string provider, bool isSuccess, double responseTime)
        {
            var currentTime = DateTime.UtcNow;
            var keyPrefix = $"provider:{provider}";

            // Store timestamp for sliding window
            await _database.SortedSetAddAsync($"{keyPrefix}:timestamps", currentTime.ToString("o"), currentTime.Ticks);

            // Remove old entries (older than 5 minutes)
            await _database.SortedSetRemoveRangeByScoreAsync($"{keyPrefix}:timestamps", double.NegativeInfinity, currentTime.AddMinutes(-5).Ticks);

            // Track success and failures
            if (isSuccess)
            {
                await _database.StringIncrementAsync($"{keyPrefix}:success");
            }
            else
            {
                await _database.StringIncrementAsync($"{keyPrefix}:failure");
            }

            // Track total requests
            await _database.StringIncrementAsync($"{keyPrefix}:total");

            // Track response time
            await _database.ListLeftPushAsync($"{keyPrefix}:responseTimes", responseTime);
            await _database.ListTrimAsync($"{keyPrefix}:responseTimes", 0, 99); // Keep last 100 response times
        }

        public async Task<ProviderMetrics> GetProviderMetricsAsync(string provider)
        {
            var keyPrefix = $"provider:{provider}";

            var total = (int)await _database.StringGetAsync($"{keyPrefix}:total");
            var failures = (int)await _database.StringGetAsync($"{keyPrefix}:failure");
            var success = (int)await _database.StringGetAsync($"{keyPrefix}:success");

            var responseTimes = (await _database.ListRangeAsync($"{keyPrefix}:responseTimes"))
                .Select(x => double.Parse(x))
                .ToList();

            var averageResponseTime = responseTimes.Any() ? responseTimes.Average() : 0.0;

            return new ProviderMetrics
            {
                Provider = provider,
                TotalRequests = total,
                FailureCount = failures,
                SuccessCount = success,
                AverageResponseTime = averageResponseTime
            };
        }
    }
}
