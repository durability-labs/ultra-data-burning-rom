using System.IO.Compression;

namespace UltraDataBurningROM.Server.Services
{
    public interface IDownloadService
    {
        void LaunchDownload(DbRom rom, DbMount mount, Action whenDone);
        bool IsDownloading(string mountId);
    }

    public class DownloadService : IDownloadService
    {
        private readonly ILogger<DownloadService> logger;
        private readonly IStorageService storageService;
        private static readonly Lock _downloadBurnLock = new Lock();
        private readonly List<string> mountsBusyDownloading = new List<string>();

        public DownloadService(ILogger<DownloadService> logger, IStorageService storageService)
        {
            this.logger = logger;
            this.storageService = storageService;
        }

        public void LaunchDownload(DbRom rom, DbMount mount, Action whenDone)
        {
            var _ = Task.Run(() =>
            {
                lock (_downloadBurnLock)
                {
                    logger.LogInformation("Download started: {cid}", rom.RomCid);
                    var node = storageService.TakeNode();
                    try
                    {
                        mountsBusyDownloading.Add(mount.Id);
                        RunDownload(node, rom, mount, whenDone);
                    }
                    finally
                    {
                        mountsBusyDownloading.Remove(mount.Id);
                        storageService.ReleaseNode(node);
                        logger.LogInformation("Download finished: {cid}", rom.RomCid);
                    }
                }
            });
        }

        public bool IsDownloading(string mountId)
        {
            return mountsBusyDownloading.Contains(mountId);
        }

        private void RunDownload(IStorageNode node, DbRom rom, DbMount mount, Action whenDone)
        {
            var cid = rom.RomCid;
            var mountZipPath = mount.GetZipFilePath();
            var mountPath = mount.Path;
            node.Download(cid, mountZipPath);

            ZipFile.ExtractToDirectory(mountZipPath, mountPath);

            whenDone();
        }
    }
}
