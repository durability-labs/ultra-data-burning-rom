namespace UltraDataBurningROM.Server.Services
{
    public interface IBucketService
    {
        Bucket GetBucket(string username);
        void DeleteFile(string username, string filename);
        string GetWriteableBucketFilePath(string username, string filename);
        void Refresh(string username);
    }

    public class BucketService : IBucketService
    {
        private readonly ulong volumeSize;
        private readonly IDatabaseService dbService;
        private readonly IUserService userService;
        private readonly IMountService mountService;

        public BucketService(IDatabaseService dbService, IUserService userService, IMountService mountService)
        {
            var envVar = Environment.GetEnvironmentVariable("BROM_ROMVOLUMESIZE");
            if (string.IsNullOrEmpty(envVar)) throw new Exception("Missing environment variable: BROM_ROMVOLUMESIZE");
            Console.WriteLine("env: " + envVar);
            volumeSize = Convert.ToUInt64(envVar);
            this.dbService = dbService;
            this.userService = userService;
            this.mountService = mountService;
        }

        public Bucket GetBucket(string username)
        {
            if (!userService.IsValid(username)) return new Bucket();
            var user = userService.GetUser(username);
            var mount = mountService.Get(user.BucketMountId);

            return new Bucket
            {
                Entries = mountService.GetFileEntries(user.BucketMountId),
                VolumeSize = volumeSize,
                State = user.BucketBurnState,
                ExpiryUtc = Utils.ToUnixTimestamp(mount.ExpiryUtc),
                RomCid = string.Empty, // todo: user.bucket burn state = burn finished? set romcid!
            };
        }

        public void DeleteFile(string username, string filename)
        {
            if (!userService.IsValid(username)) return;
            var user = userService.GetUser(username);
            mountService.DeleteFile(user.BucketMountId, filename);
        }

        public string GetWriteableBucketFilePath(string username, string filename)
        {
            if (!userService.IsValid(username)) return string.Empty;
            var user = userService.GetUser(username);
            var mount = mountService.Get(user.BucketMountId);
            return Path.Combine(mount.Path, filename);
        }

        public void Refresh(string username)
        {
            if (!userService.IsValid(username)) return;
            var user = userService.GetUser(username);
            mountService.ClearCache(user.BucketMountId);
        }
    }

    public class Bucket
    {
        public FileEntry[] Entries { get; set; } = Array.Empty<FileEntry>();
        public ulong VolumeSize { get; set; } = 0;
        public int State { get; set; } = 0;
        public long ExpiryUtc { get; set; } = 0;
        public string RomCid { get; set; } = string.Empty;
    }
}
