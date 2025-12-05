using Microsoft.AspNetCore.Mvc;

namespace UltraDataBurningROM.Server.Controllers
{
    [ApiController]
    [Route("bucket")]
    public class BucketController : ControllerBase
    {
        private static readonly List<BucketEntry> entries = new List<BucketEntry>()
        {
            new BucketEntry
            {
                Id = 101,
                Filename = "filename_1.bin",
                ByteSize = 1024 * 1024 * 4
            },
            new BucketEntry
            {
                Id = 102,
                Filename = "filename_2.bin",
                ByteSize = 1024 * 1024 * 5
            },
            new BucketEntry
            {
                Id = 103,
                Filename = "filename_3.bin",
                ByteSize = 1024 * 1024 * 6
            },
            new BucketEntry
            {
                Id = 104,
                Filename = "filename_4.bin",
                ByteSize = 1024 * 1024 * 7
            },
            new BucketEntry
            {
                Id = 105,
                Filename = "filename_5.bin",
                ByteSize = 1024 * 1024 * 8
            },
            new BucketEntry
            {
                Id = 106,
                Filename = "filename_6.bin",
                ByteSize = 1024 * 1024 * 9
            }
        };

        [HttpGet("{username}")]
        public Bucket Get(string username)
        {
            return new Bucket
            {
                VolumeSize = 1024 * 1024 * 650,
                Entries = entries.ToArray()
            };
        }

        [HttpDelete("{username}/{entryId}")]
        public void Delete(string username, ulong entryId)
        {
            entries.RemoveAll(e => e.Id == entryId);
        }



    }

    public class Bucket
    {
        public BucketEntry[] Entries { get; set; } = Array.Empty<BucketEntry>();
        public ulong VolumeSize { get; set; } = 0;
    }

    public class BucketEntry
    {
        public ulong Id { get; set; } = 0;
        public string Filename { get; set; } = string.Empty;
        public ulong ByteSize { get; set; } = 0;
    }
}
