namespace UltraDataBurningROM.Server.Services
{
    public interface IPopularContentService
    {
        void Start();
        PopularInfo GetPopularInfo();
    }

    public class PopularContentService : IPopularContentService
    {
        public const string PopContentId = "popcontentid";
        private readonly ILogger logger;
        private readonly IDatabaseService databaseService;
        private readonly IWorkerService workerService;
        private readonly IMapperService mapperService;
        private PopularInfo info = new PopularInfo();

        public PopularContentService(ILogger<PopularContentService> logger, IDatabaseService databaseService, IWorkerService workerService, IMapperService mapperService)
        {
            this.logger = logger;
            this.databaseService = databaseService;
            this.workerService = workerService;
            this.mapperService = mapperService;
        }

        public void Start()
        {
            UpdateInfo(GetOrCreateDbEntry());

            workerService.Attach(() => new PopularUpdateContext(OnResult));
        }

        public PopularInfo GetPopularInfo()
        {
            return info;
        }

        private void OnResult(DbPopContent result)
        {
            databaseService.Save(result);
            UpdateInfo(result);
            logger.LogTrace("Popular content updated.");
        }

        private DbPopContent GetOrCreateDbEntry()
        {
            var db = databaseService.Get<DbPopContent>(PopContentId);
            if (db == null)
            {
                db = new DbPopContent();
                databaseService.Save(db);
            }
            return db;
        }

        private void UpdateInfo(DbPopContent db)
        {
            info = new PopularInfo
            {
                Roms = mapperService.Map(db.RomCids),
                Tags = db.Tags
            };
        }
    }

    public class PopularInfo
    {
        public Rom[] Roms { get; set; } = Array.Empty<Rom>();
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public class PopularUpdateContext : IWorkHandler<DbRom>
    {
        private const int MaxPopularRoms = 7;
        private const int MaxTags = 7;
        private readonly List<DbRom> selected = new List<DbRom>();
        private readonly Dictionary<string, int> tagCounts = new Dictionary<string, int>();
        private int lowest = 0;
        private Action<DbPopContent> onResult;

        public PopularUpdateContext(Action<DbPopContent> onResult)
        {
            this.onResult = onResult;
        }

        public void Initialize()
        {
        }

        public void OnEntity(DbRom dbRom)
        {
            ProcessTags(dbRom);
            if (selected.Count < MaxPopularRoms)
            {
                Add(dbRom);
                return;
            }

            if (dbRom.MountCounter > lowest)
            {
                selected.Remove(selected.First(r => r.MountCounter == lowest));
                Add(dbRom);
            }
        }

        public void Finish()
        {
            onResult(new DbPopContent
            {
                Id = PopularContentService.PopContentId,
                RomCids = selected.Select(r => r.RomCid).ToArray(),
                Tags = GetTags()
            });
        }

        private void ProcessTags(DbRom dbRom)
        {
            var tags = Utils.SplitTagString(dbRom.Info.Tags);
            foreach (var tag in tags)
            {
                AddOrAdd(tag);
            }
        }

        private void AddOrAdd(string tag)
        {
            if (!tagCounts.ContainsKey(tag)) tagCounts.Add(tag, 1);
            else tagCounts[tag]++;
        }

        private string[] GetTags()
        {
            return tagCounts
                .OrderByDescending(pair => pair.Value)
                .Take(MaxTags)
                .Select(pair => pair.Key)
                .ToArray();
        }

        private void Add(DbRom dbRom)
        {
            selected.Add(dbRom);
            lowest = selected.Min(s => s.MountCounter);
        }
    }
}
