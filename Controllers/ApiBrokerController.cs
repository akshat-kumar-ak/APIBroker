using APIBroker.Interfaces;
using APIBroker.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace APIBroker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiBrokerController : ControllerBase
    {
        private readonly IApiBrokerService _apiBrokerService;

        public ApiBrokerController(IApiBrokerService apiBrokerService)
        {
            _apiBrokerService = apiBrokerService;
        }

        [HttpGet("fetch")]
        public async Task<IActionResult> GetExternalData()
        {
            var data = await _apiBrokerService.GetExternalDataAsync();
            return Ok(data);
        }
    }

}
