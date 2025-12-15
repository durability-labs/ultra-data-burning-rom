using Newtonsoft.Json;
using System.IO.Compression;

namespace UltraDataBurningROM.Server.Services
{
    public interface IBurnService
    {
        void StartBurn(string username, BurnInfo burnInfo);
        void ExtendRom(string romcid, ulong durabilityOptionId);
    }

    public class BurnService : IBurnService
    {
        private readonly ulong minBurnSize = 5 * 64 * 1024;
        private readonly IDatabaseService dbService;
        private readonly IStorageService storageService;
        private readonly IMountService mountService;
        private static readonly Lock _startBurnLock = new Lock();

        public BurnService(IDatabaseService dbService, IStorageService storageService, IMountService mountService)
        {
            this.dbService = dbService;
            this.storageService = storageService;
            this.mountService = mountService;
        }

        public void StartBurn(string username, BurnInfo burnInfo)
        {
            lock (_startBurnLock)
            {
                var user = dbService.Get<DbUser>(username);
                if (user == null) throw new Exception("Failed to find user when attempting to start burn.");
                if (user.BucketBurnState == BucketBurnState.Open)
                {
                    if (BucketIsValid(user))
                    {
                        // We start!
                        user.BucketBurnState = BucketBurnState.Starting;
                        user.BucketNewRomCid = string.Empty;
                        dbService.Save(user);

                        StartWorker(user, burnInfo);
                    }
                }
            }
        }

        public void ExtendRom(string romcid, ulong durabilityOptionId)
        {
            var rom = dbService.Get<DbRom>(romcid);
            if (rom == null) return;

            if (rom.StorageExpireUtc > (DateTime.UtcNow + TimeSpan.FromHours(48.0))) return;
            if (rom.StorageExpireUtc < DateTime.UtcNow) return;

            Task.Run(() =>
            {
                lock (_startBurnLock)
                {
                    var node = storageService.TakeNode();
                    try
                    {
                        var purchase = node.PurchaseStorage(rom.RomCid, durabilityOptionId);
                        rom.StorageExpireUtc = purchase.FinishUtc;
                        dbService.Save(rom);
                    }
                    finally
                    {
                        storageService.ReleaseNode(node);
                    }
                }
            });
        }

        private bool BucketIsValid(DbUser user)
        {
            var mount = mountService.Get(user.BucketMountId);
            if (mount == null) return false;
            if (mount.State != MountState.Bucket) return false;

            var files = mountService.GetFileEntries(user.BucketMountId);
            if (files == null || files.Length == 0) return false;

            var usedSize = 0UL;
            foreach (var file in files) usedSize += file.ByteSize;
            return usedSize > minBurnSize && usedSize <= EnvConfig.VolumeSize;
        }

        private void StartWorker(DbUser user, BurnInfo burnInfo)
        {
            Task.Run(() =>
            {
                var node = storageService.TakeNode();
                try
                {
                    Console.WriteLine("Starting burn for " + user.Username);
                    var run = new WorkerRun(dbService, mountService, node, user, burnInfo);
                    run.RunWorker();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Burn failed with exception: " + ex);
                    ReturnUserToOpenState(user);
                }
                finally
                {
                    storageService.ReleaseNode(node);
                }
            });
        }

        private void ReturnUserToOpenState(DbUser user)
        {
            lock (_startBurnLock)
            {
                // If the mount has a zip or json file, delete them.
                var mount = mountService.Get(user.BucketMountId);
                var zipFile = mount.GetZipFilePath();
                var infoFile = mount.GetInfoJsonFilePath();
                if (File.Exists(zipFile)) File.Delete(zipFile);
                if (File.Exists(infoFile)) File.Delete(infoFile);

                user.BucketBurnState = BucketBurnState.Open;
                user.BucketNewRomCid = string.Empty;
                dbService.Save(user);
            }
        }
    }

    public class WorkerRun
    {
        private readonly IDatabaseService dbService;
        private readonly IMountService mountService;
        private readonly IStorageNode storageNode;
        private readonly DbUser user;
        private readonly BurnInfo burnInfo;
        private DbMount bucketMount;
        private string uploadCid = string.Empty;
        private PurchaseResponse purchase = new PurchaseResponse();

        public WorkerRun(IDatabaseService dbService, IMountService mountService, IStorageNode storageNode, DbUser user, BurnInfo burnInfo)
        {
            this.dbService = dbService;
            this.mountService = mountService;
            this.storageNode = storageNode;
            this.user = user;
            this.burnInfo = burnInfo;

            bucketMount = mountService.Get(user.BucketMountId);
        }

        public void RunWorker()
        {
            // State = Starting.
            WriteInfoFileToBucketMount();

            SetState(BucketBurnState.Compressing);
            WriteZipFile();

            SetState(BucketBurnState.Uploading);
            UploadZipFile();

            SetState(BucketBurnState.Purchasing);
            PurchaseStorage();
            SaveNewRom();
            CreateNewUserBucketMount();

            SetState(BucketBurnState.Done);
        }

        private void SaveNewRom()
        {
            var rom = new DbRom
            {
                RomCid = purchase.PurchaseCid,
                Info = burnInfo.Fields,
                Files = mountService.GetFileEntries(bucketMount.Id),
                StorageExpireUtc = purchase.FinishUtc,
                MountCounter = 1,
                CurrentMountId = bucketMount.Id
            };
            dbService.Save(rom);

            user.BucketNewRomCid = rom.RomCid;
            dbService.Save(user);
        }

        private void CreateNewUserBucketMount()
        {
            // Current bucketmount becomes open-in-use.
            mountService.ConvertBucketMountToOpen(bucketMount.Id);

            // Make a new open bucketmount for user.
            bucketMount = mountService.CreateNewBucketMount();
            user.BucketMountId = bucketMount.Id;
            user.BucketBurnState = BucketBurnState.Open;
            dbService.Save(user);
        }

        private void PurchaseStorage()
        {
            purchase = storageNode.PurchaseStorage(uploadCid, burnInfo.DurabilityOptionId);
        }

        private void UploadZipFile()
        {
            uploadCid = storageNode.Upload(bucketMount.GetZipFilePath());
        }

        private void WriteZipFile()
        {
            ZipFile.CreateFromDirectory(bucketMount.Path, bucketMount.GetZipFilePath());
        }

        private void WriteInfoFileToBucketMount()
        {
            var infoFilePath = bucketMount.GetInfoJsonFilePath();
            File.WriteAllText(infoFilePath, JsonConvert.SerializeObject(new InfoFileContent
            {
                Header = $"Created using DurabilityLabs UltraDataBurningROM",
                Timestamp = DateTime.UtcNow.ToString("o"),
                Info = burnInfo.Fields
            }, Formatting.Indented));
        }

        private void SetState(BucketBurnState newState)
        {
            user.BucketBurnState = newState;
            dbService.Save(user);
        }

        private class InfoFileContent
        {
            public string Header { get; set; } = string.Empty;
            public string Timestamp { get; set; } = string.Empty;
            public RomInfo Info { get; set; } = new RomInfo();
        }
    }

    public static class DbMountExtensions
    {
        public static string GetZipFilePath(this DbMount mount)
        {
            return Path.Combine($"./zips/__{mount.Id.ToLowerInvariant()}.zip");
        }

        public static string GetInfoJsonFilePath(this DbMount mount)
        {
            return Path.Combine(mount.Path, "__UltraDataBurningROM__.json");
        }
    }
}
