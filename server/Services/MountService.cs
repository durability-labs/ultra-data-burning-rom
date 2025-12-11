namespace UltraDataBurningROM.Server.Services
{
    public interface IMountService
    {
        DbMount CreateNewBucketMount();
        DbMount Get(string mountId);
        FileEntry[] GetFileEntries(string mountId);
        void DeleteFile(string mountId, string filename);
        void ClearCache(string mountId);
        void ConvertBucketMountToOpen(string mountId);
        void BeginMount(string romCid);
        void EndMount(string romCid);
    }

    public class MountService : IMountService
    {
        private readonly IDatabaseService dbService;
        private readonly IDownloadService downloadService;
        private readonly string rootPath = "/mounts";
        private readonly CapMap<string, List<FileEntry>> cache = new CapMap<string, List<FileEntry>>();

        public MountService(IDatabaseService dbService, IDownloadService downloadService)
        {
            this.dbService = dbService;
            this.downloadService = downloadService;
            Directory.CreateDirectory(rootPath);
            Directory.CreateDirectory("./zips");
        }

        public DbMount CreateNewBucketMount()
        {
            return CreateNewMount(MountState.Bucket);
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

        public void ConvertBucketMountToOpen(string mountId)
        {
            var mount = Get(mountId);
            if (mount.State != MountState.Bucket) throw new Exception("Attempt to convert non-bucket mount.");
            mount.State = MountState.OpenInUse;
            dbService.Save(mount);
        }

        public void BeginMount(string romCid)
        {
            var rom = dbService.Get<DbRom>(romCid);
            if (rom == null) return;

            if (string.IsNullOrEmpty(rom.CurrentMountId))
            {
                CreateNewMountForRom(rom);
            }
            else
            {
                var mount = dbService.Get<DbMount>(rom.CurrentMountId);
                if (mount == null)
                {
                    CreateNewMountForRom(rom);
                }
                else
                {
                    OpenExistingMount(rom, mount);
                }
            }
        }

        public void EndMount(string romCid)
        {
            var rom = dbService.Get<DbRom>(romCid);
            if (rom == null) return;
            if (string.IsNullOrEmpty(rom.CurrentMountId)) return;

            var mount = dbService.Get<DbMount>(rom.CurrentMountId);
            if (mount == null) return;

            switch (mount.State)
            {
                case MountState.Bucket:
                    // Buckets are always mounted. Can't be unmounted.
                    break;
                case MountState.Downloading:
                    // We can't cancel the download. Mount will open,
                    // then it can be unmounted.
                    break;
                case MountState.OpenInUse:
                    mount.State = MountState.ClosedNotUsed;
                    dbService.Save(mount);
                    break;
                case MountState.ClosedNotUsed:
                    // Already closed. Nothing to do.
                    break;
                case MountState.Unknown:
                default:
                    throw new Exception("Unvalid state for unmounting: " + mount.State);
            }
        }

        private DbMount CreateNewMount(MountState state)
        {
            var id = Guid.NewGuid().ToString().ToLowerInvariant();
            var mount = new DbMount
            {
                Id = id,
                ExpiryUtc = DateTime.UtcNow + TimeSpan.FromHours(3.0),
                Path = Path.Combine(rootPath, id),
                State = state
            };
            dbService.Save(mount);
            Directory.CreateDirectory(mount.Path);
            return mount;
        }

        private void CreateNewMountForRom(DbRom rom)
        {
            var mount = CreateNewMount(MountState.Downloading);
            LaunchDownload(rom, mount);
        }

        private void OpenExistingMount(DbRom rom, DbMount mount)
        {
            switch (mount.State)
            {
                case MountState.Bucket:
                    // Buckets are always mounted. Can't be unmounted.
                    break;
                case MountState.Downloading:
                    // Already working on it.
                    break;
                case MountState.OpenInUse:
                    // Already good to go.
                    break;
                case MountState.ClosedNotUsed:
                    // Now in use again.
                    // do not mountCounter++
                    // users can unmount/mount endlessly to screw with popularity numbers.
                    mount.State = MountState.OpenInUse;
                    dbService.Save(mount);
                    break;
                case MountState.Unknown:
                default:
                    throw new Exception("Unvalid state for mounting: " + mount.State);
            }
        }

        private void LaunchDownload(DbRom rom, DbMount mount)
        {
            mount.State = MountState.Downloading;
            rom.CurrentMountId = mount.Id;
            rom.MountCounter++;
            dbService.Save(rom);
            dbService.Save(mount);

            downloadService.LaunchDownload(rom, mount, () => DownloadFinished(rom, mount));
        }

        private void DownloadFinished(DbRom rom, DbMount mount)
        {
            mount.State = MountState.OpenInUse;
            mount.ExpiryUtc = DateTime.UtcNow + TimeSpan.FromHours(3.0);
            dbService.Save(mount);
            dbService.Save(rom);
        }
    }
}
