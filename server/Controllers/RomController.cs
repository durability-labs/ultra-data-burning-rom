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

        [HttpGet("{username}/{romcid}/file")]
        public async Task<IActionResult> GetFile(string username, string romcid, [FromBody] FilenameRequest filenameRequest)
        {
            if (!userService.IsValid(username)) return Ok();
            var filePath = mountService.GetFilePath(romcid, filenameRequest.Filename);
            return ProvideFile(filePath);
        }

        [HttpGet("{username}/{romcid}/all")]
        public async Task<IActionResult> GetArchive(string username, string romcid)
        {
            if (!userService.IsValid(username)) return Ok();
            var filePath = mountService.GetZipFilePath(romcid);
            return ProvideFile(filePath);
        }

        [HttpPost("{username}/{romcid}/extend/{durabilityOptionId}")]
        public async Task<IActionResult> Extend(string username, string romcid, ulong durabilityOptionId)
        {
            Console.WriteLine("TODO: extend with id " + durabilityOptionId);
            return Ok();
        }

        private IActionResult ProvideFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return NotFound();
            if (!System.IO.File.Exists(filePath)) return NotFound();
            return File(System.IO.File.OpenRead(filePath), "application/octet-stream", Path.GetFileName(filePath));
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
