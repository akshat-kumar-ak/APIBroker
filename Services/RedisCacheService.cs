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

            // Store success/failure as a sorted set with timestamp
            var status = isSuccess ? "success" : "failure";
            await _database.SortedSetAddAsync($"{keyPrefix}:{status}", currentTime.ToString("o"), currentTime.Ticks);

            // Track response time using sorted set
            await _database.SortedSetAddAsync($"{keyPrefix}:responseTimes", responseTime, currentTime.Ticks);

            // Remove entries older than 5 minutes (sliding window)
            var minScore = currentTime.AddMinutes(-5).Ticks;

            await _database.SortedSetRemoveRangeByScoreAsync($"{keyPrefix}:success", double.NegativeInfinity, minScore);
            await _database.SortedSetRemoveRangeByScoreAsync($"{keyPrefix}:failure", double.NegativeInfinity, minScore);
            await _database.SortedSetRemoveRangeByScoreAsync($"{keyPrefix}:responseTimes", double.NegativeInfinity, minScore);
        }

        public async Task<ProviderMetrics> GetProviderMetricsAsync(string provider)
        {
            var currentTime = DateTime.UtcNow;
            var minScore = currentTime.AddMinutes(-5).Ticks;
            var keyPrefix = $"provider:{provider}";

            // Get counts for success, failure, and total within the last 5 minutes
            var successCount = await _database.SortedSetLengthAsync($"{keyPrefix}:success", minScore, double.PositiveInfinity);
            var failureCount = await _database.SortedSetLengthAsync($"{keyPrefix}:failure", minScore, double.PositiveInfinity);
            var totalCount = successCount + failureCount;

            // Get response times within the last 5 minutes
            var responseTimes = (await _database.SortedSetRangeByScoreAsync($"{keyPrefix}:responseTimes", minScore, double.PositiveInfinity))
                .Select(x => double.Parse(x))
                .ToList();

            var averageResponseTime = responseTimes.Any() ? responseTimes.Average() : 0.0;

            return new ProviderMetrics
            {
                Provider = provider,
                TotalRequests = (int)totalCount,
                FailureCount = (int)failureCount,
                SuccessCount = (int)successCount,
                AverageResponseTime = averageResponseTime
            };
        }
    }
}
