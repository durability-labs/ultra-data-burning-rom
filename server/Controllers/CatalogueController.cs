using Microsoft.AspNetCore.Mvc;

namespace UltraDataBurningROM.Server.Controllers
{
    [ApiController]
    [Route("catalogue")]
    public class CatalogueController : ControllerBase
    {
        private static readonly PopularInfo popular = new PopularInfo
        {
            Roms = [
                new Rom
                {
                    RomCid = "pop1",
                    Entries = [
                        new FileEntry
                        {
                            Filename = "pop1file.txt",
                            ByteSize = 123456,
                            Id = 1202
                        }
                    ],
                    MountExpiryUtc = new DateTimeOffset(DateTime.UtcNow.AddHours(3)).ToUnixTimeMilliseconds(),
                    Info = new RomInfo
                    {
                        Title = "popular1",
                        Author = "mr a",
                        Description = "everyone likes this one",
                        Tags = "popular"
                    },
                    MountState = 0
                }
            ],
            Tags =
            [
                "tag1",
                "tag2",
                "tag3",
                "tag4"
            ]
        };

        private static readonly SearchResult searchResult = new SearchResult
        {
            Roms = [
                new Rom
                {
                    RomCid = "serach1",
                    Entries = [
                        new FileEntry
                        {
                            Filename = "serach1file.txt",
                            ByteSize = 123456,
                            Id = 1202
                        }
                    ],
                    MountExpiryUtc = new DateTimeOffset(DateTime.UtcNow.AddHours(3)).ToUnixTimeMilliseconds(),
                    StorageExpiryUtc = new DateTimeOffset(DateTime.UtcNow.AddHours(3)).ToUnixTimeMilliseconds(),
                    Info = new RomInfo
                    {
                        Title = "serach1",
                        Author = "mr a",
                        Description = "everyone finds this one",
                        Tags = "search"
                    },
                    MountState = 0
                },
                new Rom
                {
                    RomCid = "serach2",
                    Entries = [
                        new FileEntry
                        {
                            Filename = "serach2file.txt",
                            ByteSize = 123456,
                            Id = 1202
                        }
                    ],
                    MountExpiryUtc = new DateTimeOffset(DateTime.UtcNow.AddHours(3)).ToUnixTimeMilliseconds(),
                    StorageExpiryUtc = new DateTimeOffset(DateTime.UtcNow.AddDays(3)).ToUnixTimeMilliseconds(),
                    Info = new RomInfo
                    {
                        Title = "serach2",
                        Author = "mr a",
                        Description = "everyone finds this one",
                        Tags = "search"
                    },
                    MountState = 0
                }
            ]
        };

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(popular);
        }

        [HttpPost("search/{query}")]
        public async Task<IActionResult> Search(string query)
        {
            Console.WriteLine("search query: " + query);
            return Ok(searchResult);
        }
    }

    public class PopularInfo
    {
        public Rom[] Roms { get; set; } = Array.Empty<Rom>();
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public class SearchResult
    {
        public Rom[] Roms { get; set; } = Array.Empty<Rom>();
    }
}
