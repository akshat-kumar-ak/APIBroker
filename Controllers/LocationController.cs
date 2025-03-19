using APIBroker.Interfaces;
using APIBroker.Services;
using Microsoft.AspNetCore.Mvc;

namespace APIBroker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly IProviderBrokerService _providerBrokerService;

        public LocationController(IProviderBrokerService providerBrokerService)
        {
            _providerBrokerService = providerBrokerService;
        }

        [HttpGet("{ip}")]
        public async Task<IActionResult> GetLocation(string ip)
        {
            try
            {
                var location = await _providerBrokerService.GetLocationAsync(ip);
                return Ok(location);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
