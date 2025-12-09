using Microsoft.AspNetCore.Mvc;

namespace UltraDataBurningROM.Server.Controllers
{
    [ApiController]
    [Route("rom")]
    public class RomController : ControllerBase
    {
        private static readonly Rom rom = new Rom()
        {
            RomCid = "romcid1",
            Mounted = true,
            Info = new RomInfo()
            {
                Title = "title1",
                Author = "author1",
                Tags = "tags1",
                Description = "description1"
            },
            Entries =
            [
                new BucketEntry()
                {
                    Id = 101,
                    ByteSize = 1234567,
                    Filename = "file.bin"
                }
            ],
            MountExpiryUtc = new DateTimeOffset(DateTime.UtcNow.AddHours(3)).ToUnixTimeMilliseconds(),
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
            rom.Mounted = true;
            return Ok(rom);
        }

        [HttpPost("{username}/{romcid}/unmount")]
        public async Task<IActionResult> Unmount(string username, string romcid)
        {
            rom.Mounted = false;
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
        public bool Mounted { get; set; } = false;
        public RomInfo Info { get; set; } = new RomInfo();
        public BucketEntry[] Entries { get; set; } = Array.Empty<BucketEntry>();
        public long MountExpiryUtc { get; set; } = 0;
        public long StorageExpiryUtc { get; set; } = 0;
    }

    [Serializable]
    public class BurnInfo
    {
        public RomInfo Fields { get; set; } = new RomInfo();
        public ulong DurabilityOptionId { get; set; }
    }

    [Serializable]
    public class RomInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
