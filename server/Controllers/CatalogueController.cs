using Microsoft.AspNetCore.Mvc;
using UltraDataBurningROM.Server.Services;

namespace UltraDataBurningROM.Server.Controllers
{
    [ApiController]
    [Route("catalogue")]
    public class CatalogueController : ControllerBase
    {
        private readonly IPopularContentService popularContentService;
        private readonly ISearchService searchService;

        public CatalogueController(IPopularContentService popularContentService, ISearchService searchService)
        {
            this.popularContentService = popularContentService;
            this.searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(popularContentService.GetPopularInfo());
        }

        [HttpPost("search/{query}")]
        public async Task<IActionResult> Search(string query)
        {
            return Ok(searchService.Search(query));
        }
    }
}
