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

        public CleanupService(IWorkerService workerService, IDatabaseService dbService)
        {
            this.workerService = workerService;
            this.dbService = dbService;
        }

        public void Start()
        {
            workerService.Attach(() => new CleanupContext(dbService));
        }
    }

    public class CleanupContext : IWorkHandler<DbMount>
    {
        private readonly IDatabaseService dbService;
        private readonly List<DbMount> toCleanup = new List<DbMount>();

        public CleanupContext(IDatabaseService dbService)
        {
            this.dbService = dbService;
        }

        public void Initialize()
        {
        }

        public void OnEntity(DbMount mount)
        {
            if (
                mount.State == MountState.OpenInUse &&
                mount.ExpiryUtc < DateTime.UtcNow
            )
            {
                mount.State = MountState.ClosedNotUsed;
                dbService.Save(mount);
            }
            else if(
                mount.State == MountState.ClosedNotUsed &&
                (mount.ExpiryUtc + TimeSpan.FromHours(3.0)) < DateTime.UtcNow
            )
            {
                toCleanup.Add(mount);
            }
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
    }
}
