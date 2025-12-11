using System.IO.Compression;

namespace UltraDataBurningROM.Server.Services
{
    public interface IDownloadService
    {
        void LaunchDownload(DbRom rom, DbMount mount, Action whenDone);
    }

    public class DownloadService : IDownloadService
    {
        private readonly IStorageService storageService;
        private static readonly Lock _downloadBurnLock = new Lock();

        public DownloadService(IStorageService storageService)
        {
            this.storageService = storageService;
        }

        public void LaunchDownload(DbRom rom, DbMount mount, Action whenDone)
        {
            var _ = Task.Run(() =>
            {
                lock (_downloadBurnLock)
                {
                    var node = storageService.TakeNode();
                    try
                    {
                        RunDownload(node, rom, mount, whenDone);
                    }
                    finally
                    {
                        storageService.ReleaseNode(node);
                    }
                }
            });
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
