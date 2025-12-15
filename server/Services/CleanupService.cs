namespace UltraDataBurningROM.Server.Services
{
    public interface ICleanupService
    {
        void Start();
    }

    public class CleanupService : ICleanupService
    {
        private readonly ILogger logger;
        private readonly IWorkerService workerService;
        private readonly IDatabaseService dbService;
        private readonly IDownloadService downloadService;

        public CleanupService(ILogger<CleanupService> logger, IWorkerService workerService, IDatabaseService dbService, IDownloadService downloadService)
        {
            this.logger = logger;
            this.workerService = workerService;
            this.dbService = dbService;
            this.downloadService = downloadService;
        }

        public void Start()
        {
            workerService.Attach(() => new CleanupContext(logger, dbService, downloadService));
        }
    }

    public class CleanupContext : IWorkHandler<DbMount>
    {
        private readonly ILogger logger;
        private readonly IDatabaseService dbService;
        private readonly IDownloadService downloadService;
        private readonly List<DbMount> toCleanup = new List<DbMount>();

        public CleanupContext(ILogger logger, IDatabaseService dbService, IDownloadService downloadService)
        {
            this.logger = logger;
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
                logger.LogInformation("Cleanup: {path}", item.Path);
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
                logger.LogInformation("Closed expired mount");
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
                logger.LogInformation("Closed stuck downloading mount");
            }
        }
    }
}
