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
        private readonly ILogger<SearchService> logger;
        private readonly IMapperService mapperService;
        private readonly IWorkerService workerService;
        private SearchIndex index = new SearchIndex();

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
                matches = tokens
                    .SelectMany(index.GetMatches)
                    .Take(30)
                    .ToArray();
            }
            return new SearchResult
            {
                Roms = mapperService.Map(matches)
            };
        }

        private void OnResult(SearchIndex newIndex)
        {
            lock (_lock)
            {
                index = newIndex;
                logger.LogTrace("Search index updated.");
            }
        }
    }

    public class SearchIndex
    {
        private readonly Dictionary<string, List<string>> index = new Dictionary<string, List<string>>();

        public void AddRomForToken(string token, string romCid)
        {
            if (index.TryGetValue(token, out List<string>? value))
            {
                if (value != null)
                {
                    value.Add(romCid);
                    return;
                }
            }
            index[token] = new List<string> { romCid };
        }

        public string[] GetMatches(string token)
        {
            if (index.TryGetValue(token, out var matches))
            {
                return matches.ToArray();
            }
            return Array.Empty<string>();
        }
    }

    public class SearchResult
    {
        public Rom[] Roms { get; set; } = Array.Empty<Rom>();
    }

    public class SearchIndexer : IWorkHandler<DbRom>
    {
        private readonly Action<SearchIndex> onResult;
        private readonly SearchIndex newIndex = new SearchIndex();

        public SearchIndexer(Action<SearchIndex> onResult)
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
                newIndex.AddRomForToken(token, entity.RomCid);
            }
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
