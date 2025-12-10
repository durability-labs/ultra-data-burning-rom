using Microsoft.AspNetCore.Mvc;
using UltraDataBurningROM.Server.Services;

namespace UltraDataBurningROM.Server.Controllers
{
    [ApiController]
    [Route("durability")]
    public class DurabilityController : ControllerBase
    {
        private readonly IStorageService storageService;

        public DurabilityController(IStorageService storageService)
        {
            this.storageService = storageService;
        }


        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(storageService.GetDurability());
        }
    }
}
