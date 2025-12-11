using Microsoft.AspNetCore.Mvc;
using UltraDataBurningROM.Server.Services;

namespace UltraDataBurningROM.Server.Controllers
{
    [ApiController]
    [Route("catalogue")]
    public class CatalogueController : ControllerBase
    {
        public CatalogueController(IPopularContentService popularContentService)
        {
            this.popularContentService = popularContentService;
        }

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
        private readonly IPopularContentService popularContentService;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(popularContentService.GetPopularInfo());
        }

        [HttpPost("search/{query}")]
        public async Task<IActionResult> Search(string query)
        {
            Console.WriteLine("search query: " + query);
            return Ok(searchResult);
        }
    }

    public class SearchResult
    {
        public Rom[] Roms { get; set; } = Array.Empty<Rom>();
    }
}
