using Microsoft.AspNetCore.Mvc;
using UltraDataBurningROM.Server.Services;

namespace UltraDataBurningROM.Server.Controllers
{
    [ApiController]
    [Route("rom")]
    public class RomController : ControllerBase
    {
        private static readonly Rom rom = new Rom()
        {
            RomCid = "romcid1",
            MountState = 0,
            Info = new RomInfo()
            {
                Title = "title1",
                Author = "author1",
                Tags = "tags1",
                Description = "description1"
            },
            Entries =
            [
                new FileEntry()
                {
                    ByteSize = 1234567,
                    Filename = "file.bin"
                }
            ],
            MountExpiryUtc = 0,
            StorageExpiryUtc = new DateTimeOffset(DateTime.UtcNow.AddHours(3)).ToUnixTimeMilliseconds()
        };

        [HttpGet("{username}/{romcid}")]
        public async Task<IActionResult> Get(string username, string romcid)
        {
            return Ok(rom);
        }

        [HttpPost("{username}/{romcid}/mount")]
        public async Task<IActionResult> Mount(string username, string romcid)
        {
            if (rom.MountState == MountState.ClosedNotUsed)
            {
                rom.MountState = MountState.Downloading;
                rom.MountExpiryUtc = new DateTimeOffset(DateTime.UtcNow.AddHours(3)).ToUnixTimeMilliseconds();
                var _ = Task.Run(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    rom.MountState = MountState.OpenInUse;
                });
            }

            return Ok(rom);
        }

        [HttpPost("{username}/{romcid}/unmount")]
        public async Task<IActionResult> Unmount(string username, string romcid)
        {
            if (rom.MountState == MountState.OpenInUse)
            {
                rom.MountState = MountState.ClosedNotUsed;
            }
            return Ok(rom);
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
    }

    public class Rom
    {
        public string RomCid { get; set; } = string.Empty;
        public MountState MountState { get; set; } = MountState.Unknown;
        public RomInfo Info { get; set; } = new RomInfo();
        public FileEntry[] Entries { get; set; } = Array.Empty<FileEntry>();
        public long MountExpiryUtc { get; set; } = 0;
        public long StorageExpiryUtc { get; set; } = 0;
    }
}
