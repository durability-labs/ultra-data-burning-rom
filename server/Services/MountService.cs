namespace UltraDataBurningROM.Server.Services
{
    public interface IMountService
    {
        DbMount CreateNewBucketMount();
        DbMount Get(string mountId);
        FileEntry[] GetFileEntries(string mountId);
        void DeleteFile(string mountId, string filename);
        void ClearCache(string mountId);
    }

    public class MountService : IMountService
    {
        private readonly IDatabaseService dbService;
        private readonly string rootPath = "/mounts";
        private readonly CapMap<string, List<FileEntry>> cache = new CapMap<string, List<FileEntry>>();

        public MountService(IDatabaseService dbService)
        {
            this.dbService = dbService;

            Directory.CreateDirectory(rootPath);
        }

        public DbMount CreateNewBucketMount()
        {
            var id = Guid.NewGuid().ToString().ToLowerInvariant();
            var mount = new DbMount
            {
                Id = id,
                ExpiryUtc = DateTime.UtcNow + TimeSpan.FromHours(3.0),
                Path = Path.Combine(rootPath, id),
                State = MountState.Bucket
            };
            dbService.Save(mount);
            Directory.CreateDirectory(mount.Path);
            return mount;
        }

        public DbMount Get(string mountId)
        {
            var mount = dbService.Get<DbMount>(mountId);
            if (mount == null) throw new Exception("Unable to found mount by id: " + mountId);
            return mount;
        }

        public FileEntry[] GetFileEntries(string mountId)
        {
            if (cache.TryGetValue(mountId, out var entries))
            {
                if (entries != null) return entries.ToArray();
            }

            var mount = Get(mountId);
            var files = Directory.GetFiles(mount.Path);
            var newEntries = files.Select(f =>
            {
                var info = new FileInfo(f);
                return new FileEntry
                {
                    Filename = Path.GetFileName(f),
                    ByteSize = Convert.ToUInt64(info.Length)
                };
            }).ToList();

            cache.Add(mountId, newEntries);
            return newEntries.ToArray();
        }

        public void DeleteFile(string mountId, string filename)
        {
            if (cache.TryGetValue(mountId, out var entries))
            {
                if (entries != null)
                {
                    entries.RemoveAll(e => e.Filename == filename);
                }
            }
            var mount = Get(mountId);
            if (mount.State != MountState.Bucket) throw new Exception("Attempt to delete file from non-bucket mount");

            var path = Path.Combine(mount.Path, filename);
            if (File.Exists(path)) File.Delete(path);
        }

        public void ClearCache(string mountId)
        {
            cache.ClearKey(mountId);
        }
    }
}
