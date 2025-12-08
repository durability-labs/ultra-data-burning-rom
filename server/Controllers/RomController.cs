using Microsoft.AspNetCore.Mvc;

namespace UltraDataBurningROM.Server.Controllers
{
    [ApiController]
    [Route("rom")]
    public class RomController : ControllerBase
    {
        [HttpGet("{username}/{romcid}")]
        public async Task<IActionResult> Get(string username, string romcid)
        {
            Console.WriteLine("romcid get: " + username + romcid);
            return Ok();
        }
    }
}
