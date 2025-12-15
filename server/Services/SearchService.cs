using UltraDataBurningROM.Server.Controllers;

namespace UltraDataBurningROM.Server.Services
{
    public interface ISearchService
    {
        void Start();
        SearchResult Search(string query);
    }

    public class SearchService : ISearchService
    {
        private readonly Lock _lock = new Lock();
        private Dictionary<string, List<string>> index = new Dictionary<string, List<string>>();
        private readonly ILogger<SearchService> logger;
        private readonly IMapperService mapperService;
        private readonly IWorkerService workerService;

        public SearchService(ILogger<SearchService> logger, IMapperService mapperService, IWorkerService workerService)
        {
            this.logger = logger;
            this.mapperService = mapperService;
            this.workerService = workerService;
        }

        public void Start()
        {
            workerService.Attach(() => new SearchIndexer(OnResult));
        }

        public SearchResult Search(string query)
        {
            var asRomCid = mapperService.Map(query);
            if (asRomCid != null)
            {
                return new SearchResult
                {
                    Roms = [asRomCid]
                };
            }

            var tokens = Utils.SplitTagString(query).Take(10).Select(t => t.ToLowerInvariant()).ToArray();
            var matches = Array.Empty<string>();
            lock (_lock)
            {
                matches = index
                    .Where(i => tokens.Contains(i.Key))
                    .SelectMany(i => i.Value)
                    .Take(30)
                    .ToArray();
            }
            return new SearchResult
            {
                Roms = mapperService.Map(matches)
            };
        }

        private void OnResult(Dictionary<string, List<string>> newIndex)
        {
            lock (_lock)
            {
                index = newIndex;
                logger.LogInformation("Search index updated.");
            }
        }
    }

    public class SearchResult
    {
        public Rom[] Roms { get; set; } = Array.Empty<Rom>();
    }

    public class SearchIndexer : IWorkHandler<DbRom>
    {
        private readonly Action<Dictionary<string, List<string>>> onResult;
        private readonly Dictionary<string, List<string>> newIndex = new Dictionary<string, List<string>>();

        public SearchIndexer(Action<Dictionary<string, List<string>>> onResult)
        {
            this.onResult = onResult;
        }

        public void Initialize()
        {
        }

        public void OnEntity(DbRom entity)
        {
            var tokens = GatherTokens(entity.Info);
            foreach (var token in tokens)
            {
                Add(token, entity.RomCid);
            }
        }

        private void Add(string token, string romCid)
        {
            if (newIndex.TryGetValue(token, out List<string>? value))
            {
                if (value != null)
                {
                    value.Add(romCid);
                    return;
                }
            }
            newIndex[token] = new List<string> { romCid };
        }

        private string[] GatherTokens(RomInfo info)
        {
            var result = new List<string>();
            result.AddRange(Utils.SplitTagString(info.Title.ToLowerInvariant()));
            result.AddRange(Utils.SplitTagString(info.Author.ToLowerInvariant()));
            result.AddRange(Utils.SplitTagString(info.Tags.ToLowerInvariant()).Take(10));
            result.AddRange(Utils.SplitTagString(info.Description.ToLowerInvariant()).Take(10));
            return result.ToArray();
        }

        public void Finish()
        {
            onResult(newIndex);
        }
    }
}
