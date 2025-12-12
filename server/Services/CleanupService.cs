namespace UltraDataBurningROM.Server.Services
{
    public interface ICleanupService
    {
        void Start();
    }

    public class CleanupService : ICleanupService
    {
        private readonly IWorkerService workerService;
        private readonly IDatabaseService dbService;
        private readonly IDownloadService downloadService;

        public CleanupService(IWorkerService workerService, IDatabaseService dbService, IDownloadService downloadService)
        {
            this.workerService = workerService;
            this.dbService = dbService;
            this.downloadService = downloadService;
        }

        public void Start()
        {
            workerService.Attach(() => new CleanupContext(dbService, downloadService));
        }
    }

    public class CleanupContext : IWorkHandler<DbMount>
    {
        private readonly IDatabaseService dbService;
        private readonly IDownloadService downloadService;
        private readonly List<DbMount> toCleanup = new List<DbMount>();

        public CleanupContext(IDatabaseService dbService, IDownloadService downloadService)
        {
            this.dbService = dbService;
            this.downloadService = downloadService;
        }

        public void Initialize()
        {
        }

        public void OnEntity(DbMount mount)
        {
            CloseOldOpenMount(mount);
            MarkForCleanupOldClosedMount(mount);
            CloseStuckDownloadingMount(mount);
        }

        public void Finish()
        {
            foreach (var item in toCleanup)
            {
                dbService.Delete<DbMount>(item.Id);
            }

            foreach (var item in toCleanup)
            {
                if (Directory.Exists(item.Path)) Directory.Delete(item.Path, true);
                if (File.Exists(item.GetZipFilePath())) File.Delete(item.GetZipFilePath());
            }
        }

        private void CloseOldOpenMount(DbMount mount)
        {
            if (
                mount.State == MountState.OpenInUse &&
                mount.ExpiryUtc < DateTime.UtcNow
            )
            {
                mount.State = MountState.ClosedNotUsed;
                dbService.Save(mount);
            }
        }

        private void MarkForCleanupOldClosedMount(DbMount mount)
        {
            if (
                mount.State == MountState.ClosedNotUsed &&
                (mount.ExpiryUtc + TimeSpan.FromHours(3.0)) < DateTime.UtcNow
            )
            {
                toCleanup.Add(mount);
            }
        }

        private void CloseStuckDownloadingMount(DbMount mount)
        {
            if (
                mount.State == MountState.Downloading &&
                !downloadService.IsDownloading(mount.Id)
            )
            {
                mount.State = MountState.ClosedNotUsed;
                dbService.Save(mount);
            }
        }
    }
}
