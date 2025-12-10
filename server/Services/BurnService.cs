using Newtonsoft.Json;
using System.IO.Compression;

namespace UltraDataBurningROM.Server.Services
{
    public interface IBurnService
    {
        void StartBurn(string username, BurnInfo burnInfo);
    }

    public class BurnService : IBurnService
    {
        private readonly IDatabaseService dbService;
        private readonly IStorageService storageService;
        private readonly IMountService mountService;
        private static readonly object _startBurnLock = new object();

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
                    if (!BucketExceedsLimit(user))
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

        private bool BucketExceedsLimit(DbUser user)
        {
            var files = mountService.GetFileEntries(user.BucketMountId);
            var usedSize = 0UL;
            foreach (var file in files) usedSize += file.ByteSize; // This is no .Sum for ulongs :o
            return usedSize > EnvConfig.VolumeSize;
        }

        private void StartWorker(DbUser user, BurnInfo burnInfo)
        {
            Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("Starting burn for " + user.Username);
                    var run = new WorkerRun(dbService, mountService, storageService, user, burnInfo);
                    run.RunWorker();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Burn failed with exception: " + ex);
                    ReturnUserToOpenState(user);
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
        private readonly IStorageService storageService;
        private readonly DbUser user;
        private readonly BurnInfo burnInfo;
        private DbMount bucketMount;
        private string uploadCid = string.Empty;
        private PurchaseResponse purchase = new PurchaseResponse();

        public WorkerRun(IDatabaseService dbService, IMountService mountService, IStorageService storageService, DbUser user, BurnInfo burnInfo)
        {
            this.dbService = dbService;
            this.mountService = mountService;
            this.storageService = storageService;
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
            purchase = storageService.PurchaseStorage(uploadCid, burnInfo.DurabilityOptionId);
        }

        private void UploadZipFile()
        {
            uploadCid = storageService.Upload(bucketMount.GetZipFilePath());
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
            return Path.Combine($"/zips/__{mount.Id.ToLowerInvariant()}.zip");
        }

        public static string GetInfoJsonFilePath(this DbMount mount)
        {
            return Path.Combine(mount.Path, "__UltraDataBurningROM__.json");
        }
    }
}
