namespace UltraDataBurningROM.Server.Services
{
    public interface IMapperService
    {
        Rom[] Map(string[] romCids);
        Rom? Map(string romCid);
    }

    public class MapperService : IMapperService
    {
        private readonly IDatabaseService databaseService;

        public MapperService(IDatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        public Rom[] Map(string[] romCids)
        {
            return romCids.Select(Map).Where(r => r != null).Cast<Rom>().ToArray();
        }

        public Rom? Map(string romCid)
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

    public class Rom
    {
        public string RomCid { get; set; } = string.Empty;
        public MountState MountState { get; set; } = MountState.Unknown;
        public RomInfo Info { get; set; } = new RomInfo();
        public FileEntry[] Entries { get; set; } = Array.Empty<FileEntry>();
        public long MountExpiryUtc { get; set; } = 0;
        public long StorageExpiryUtc { get; set; } = 0;
    }
}
