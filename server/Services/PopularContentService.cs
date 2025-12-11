using UltraDataBurningROM.Server.Controllers;

namespace UltraDataBurningROM.Server.Services
{
    public interface IPopularContentService
    {
        void Start();
        PopularInfo GetPopularInfo();
        void Stop();
    }

    public class PopularContentService : IPopularContentService
    {
        private readonly TimeSpan UpdateFrequency = TimeSpan.FromMinutes(30.0);
        private const string PopContentId = "popcontentid";
        private readonly IDatabaseService databaseService;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private Task worker = Task.CompletedTask;
        private PopularInfo info = new PopularInfo();

        public PopularContentService(IDatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        public void Start()
        {
            Log("Starting PopularContentService...");
            worker = StartWorker();
            Initialize();
        }

        public PopularInfo GetPopularInfo()
        {
            return info;
        }

        public void Stop()
        {
            Log("Stopping PopularContentService...");
            cts.Cancel();
            worker.Wait();
        }

        private void Initialize()
        {
            var db = GetOrCreateDbEntry();
            UpdateInfo(db);
        }

        private DbPopContent GetOrCreateDbEntry()
        {
            var db = databaseService.Get<DbPopContent>(PopContentId);
            if (db == null)
            {
                db = GenerateUpdate();
                databaseService.Save(db);
            }
            return db;
        }

        private Task StartWorker()
        {
            return Task.Run(() =>
            {
                try
                {
                    Worker();
                }
                catch (Exception ex)
                {
                    Log("Exception in PopularContentService worker: " + ex);
                }
            });
        }

        private void Worker()
        {
            while (!cts.IsCancellationRequested)
            {
                if (!cts.Token.WaitHandle.WaitOne(UpdateFrequency))
                {
                    var db = GenerateUpdate();
                    databaseService.Save(db);
                    UpdateInfo(db);
                }
            }
        }

        private void UpdateInfo(DbPopContent db)
        {
            info = new PopularInfo
            {
                Roms = Map(db.RomCids),
                Tags = db.Tags
            };
        }

        private DbPopContent GenerateUpdate()
        {
            Log("Updating popular content...");
            var context = new PopularUpdateContext();
            databaseService.Iterate<DbRom>(context.ProcessRom);
            return context.GetResult();
        }

        private Rom[] Map(string[] romCids)
        {
            return romCids.Select(Map).Where(r => r != null).Cast<Rom>().ToArray();
        }

        private Rom? Map(string romCid)
        {
            var dbRom = databaseService.Get<DbRom>(romCid);
            if (dbRom == null) return null;

            var mountState = GetMountState(dbRom);
            var mountExpiry = GetMountExpiry(dbRom);

            return new Rom
            {
                RomCid = romCid,
                Info = dbRom.Info,
                Entries = dbRom.Files,
                MountExpiryUtc = mountExpiry,
                MountState = mountState,
                StorageExpiryUtc = Utils.ToUnixTimestamp(dbRom.StorageExpireUtc)
            };
        }

        private MountState GetMountState(DbRom dbRom)
        {
            if (string.IsNullOrEmpty(dbRom.CurrentMountId)) return MountState.ClosedNotUsed;
            var mount = databaseService.Get<DbMount>(dbRom.CurrentMountId);
            if (mount == null) return MountState.ClosedNotUsed;
            return mount.State;
        }

        private long GetMountExpiry(DbRom dbRom)
        {
            if (string.IsNullOrEmpty(dbRom.CurrentMountId)) return 0;
            var mount = databaseService.Get<DbMount>(dbRom.CurrentMountId);
            if (mount == null) return 0;
            return Utils.ToUnixTimestamp(mount.ExpiryUtc);
        }

        private void Log(string msg)
        {
            Console.WriteLine(msg);
        }
    }

    public class PopularInfo
    {
        public Rom[] Roms { get; set; } = Array.Empty<Rom>();
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public class PopularUpdateContext
    {
        private const int MaxPopularRoms = 7;
        private const int MaxTags = 7;
        private readonly List<DbRom> selected = new List<DbRom>();
        private readonly Dictionary<string, int> tagCounts = new Dictionary<string, int>();
        private int lowest = 0;

        public void ProcessRom(DbRom dbRom)
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

        public DbPopContent GetResult()
        {
            return new DbPopContent
            {
                RomCids = selected.Select(r => r.RomCid).ToArray(),
                Tags = GetTags()
            };
        }

        private void ProcessTags(DbRom dbRom)
        {
            var tags = Utils.SplitTagString(dbRom.Info.Tags);
            foreach (var tag in tags)
            {
                Console.WriteLine("popular tag: " + tag);
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
