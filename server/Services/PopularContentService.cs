using UltraDataBurningROM.Server.Controllers;

namespace UltraDataBurningROM.Server.Services
{
    public interface IPopularContentService
    {
        PopularInfo GetPopularInfo();
    }

    public class PopularContentService : IPopularContentService
    {
        private const string PopContentId = "popcontentid";
        private readonly IDatabaseService databaseService;
        private PopularInfo info = new PopularInfo();

        public PopularContentService(IDatabaseService databaseService)
        {
            this.databaseService = databaseService;

            Initialize();
        }

        public PopularInfo GetPopularInfo()
        {
            return info;
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

        private DbPopContent GenerateUpdate()
        {
            // Scan the DbRoms. Find the most mounted ones.
        }

        private void UpdateInfo(DbPopContent db)
        {
            info = new PopularInfo
            {
                Roms = Map(db.RomCids),
                Tags = db.Tags
            };
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
    }

    public class PopularInfo
    {
        public Rom[] Roms { get; set; } = Array.Empty<Rom>();
        public string[] Tags { get; set; } = Array.Empty<string>();
    }
}
