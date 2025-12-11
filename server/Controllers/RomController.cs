using Microsoft.AspNetCore.Mvc;
using UltraDataBurningROM.Server.Services;

namespace UltraDataBurningROM.Server.Controllers
{
    [ApiController]
    [Route("rom")]
    public class RomController : ControllerBase
    {
        private readonly IUserService userService;
        private readonly IMapperService mapperService;
        private readonly IMountService mountService;

        public RomController(IUserService userService, IMapperService mapperService, IMountService mountService)
        {
            this.userService = userService;
            this.mapperService = mapperService;
            this.mountService = mountService;
        }

        [HttpGet("{username}/{romcid}")]
        public async Task<IActionResult> Get(string username, string romcid)
        {
            if (!userService.IsValid(username)) return EmptyRom();
            return Rom(romcid);
        }

        [HttpPost("{username}/{romcid}/mount")]
        public async Task<IActionResult> Mount(string username, string romcid)
        {
            if (!userService.IsValid(username)) return EmptyRom();

            mountService.BeginMount(romcid);

            return Rom(romcid);
        }

        [HttpPost("{username}/{romcid}/unmount")]
        public async Task<IActionResult> Unmount(string username, string romcid)
        {
            if (!userService.IsValid(username)) return EmptyRom();

            mountService.EndMount(romcid);

            return Rom(romcid);
        }

        [HttpGet("{username}/{romcid}/file/{entryId}")]
        public async Task<IActionResult> GetFile(string username, string romcid, ulong entryId)
        {
            // download file
            return Ok();
        }

        [HttpGet("{username}/{romcid}/all")]
        public async Task<IActionResult> GetArchive(string username, string romcid)
        {
            // download archive
            return Ok();
        }

        [HttpPost("{username}/{romcid}/extend/{durabilityOptionId}")]
        public async Task<IActionResult> Extend(string username, string romcid, ulong durabilityOptionId)
        {
            Console.WriteLine("extend with id " + durabilityOptionId);
            return Ok();
        }

        private IActionResult Rom(string romcid)
        {
            var rom = mapperService.Map(romcid);
            if (rom == null) return EmptyRom();
            return Ok(rom);
        }

        private IActionResult EmptyRom()
        {
            return Ok(new Rom());
        }
    }
}
