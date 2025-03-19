using APIBroker.Interfaces;
using APIBroker.Models;
using APIBroker.Services;
using Microsoft.AspNetCore.Mvc;

namespace APIBroker.Controllers
{
    [ApiController]
    [Route("api/broker")]
    public class RedisController : ControllerBase
    {
        private readonly IRedisCacheService _cacheService;

        public RedisController(IRedisCacheService cacheService)
        {
            _cacheService = cacheService;
        }

        [HttpPost("track")]
        public async Task<IActionResult> TrackProvider([FromBody] ProviderTrackRequest request)
        {
            await _cacheService.TrackProviderMetricsAsync(request.Provider, request.IsSuccess, request.ResponseTime);
            return Ok("Tracked");
        }

        [HttpGet("metrics/{provider}")]
        public async Task<IActionResult> GetMetrics(string provider)
        {
            var metrics = await _cacheService.GetProviderMetricsAsync(provider);
            return Ok(metrics);
        }
    }

}
