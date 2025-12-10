namespace UltraDataBurningROM.Server.Services
{
    public interface IBucketService
    {
        Bucket GetBucket(string username);
        void DeleteFile(string username, string filename);
        bool IsBucketOpen(string username);
        string GetWriteableBucketFilePath(string username, string filename);
        void Refresh(string username);
        void StartBurn(string username, BurnInfo burnInfo);
        void ClearNewRomCid(string username);
    }

    public class BucketService : IBucketService
    {
        private readonly ulong volumeSize = EnvConfig.VolumeSize;
        private readonly IUserService userService;
        private readonly IMountService mountService;
        private readonly IBurnService burnService;

        public BucketService(IUserService userService, IMountService mountService, IBurnService burnService)
        {
            this.userService = userService;
            this.mountService = mountService;
            this.burnService = burnService;
        }

        public Bucket GetBucket(string username)
        {
            if (!userService.IsValid(username)) return new Bucket();
            var user = userService.GetUser(username);
            var mount = mountService.Get(user.BucketMountId);

            var entries = mountService.GetFileEntries(user.BucketMountId);
            var expiry = GetExpiryUnixTimestamp(entries, mount);

            return new Bucket
            {
                Entries = entries,
                VolumeSize = volumeSize,
                State = user.BucketBurnState,
                ExpiryUtc = expiry,
                RomCid = user.BucketNewRomCid
            };
        }

        public void DeleteFile(string username, string filename)
        {
            if (!userService.IsValid(username)) return;
            var user = userService.GetUser(username);
            mountService.DeleteFile(user.BucketMountId, filename);
        }

        public bool IsBucketOpen(string username)
        {
            if (!userService.IsValid(username)) return false;
            var user = userService.GetUser(username);
            return user.BucketBurnState == BucketBurnState.Open;
        }

        public string GetWriteableBucketFilePath(string username, string filename)
        {
            if (!userService.IsValid(username)) return string.Empty;
            var user = userService.GetUser(username);
            var mount = mountService.Get(user.BucketMountId);

            var path = Path.Combine(mount.Path, filename);
            var pathInvariant = path.ToLowerInvariant();

            // We don't accept this if it would clash with one of our system files.
            // (invariance important for filesystems that don't distinguish.)
            if (pathInvariant == mount.GetInfoJsonFilePath().ToLowerInvariant()) return string.Empty;
            if (pathInvariant == mount.GetZipFilePath().ToLowerInvariant()) return string.Empty;
            return path;
        }

        public void Refresh(string username)
        {
            if (!userService.IsValid(username)) return;
            var user = userService.GetUser(username);
            mountService.ClearCache(user.BucketMountId);
        }

        public void StartBurn(string username, BurnInfo burnInfo)
        {
            if (!userService.IsValid(username)) return;
            burnService.StartBurn(username, burnInfo);
        }

        public void ClearNewRomCid(string username)
        {
            if (!userService.IsValid(username)) return;
            var user = userService.GetUser(username);
            if (user.BucketBurnState == BucketBurnState.Done)
            {
                // When the burn-state is set to Done, the BurnService
                // will immediately associate a new mount as bucketmount to this user.
                // Turning the old one into a normal open mount.
                // All we have to do here is clear the burn state from done back to open,
                // and clear the new romCid, so we're back in the original state:
                // An empty bucket-mount that is in state 'Open'.
                user.BucketBurnState = BucketBurnState.Open;
                user.BucketNewRomCid = string.Empty;
                userService.SaveUser(user);
            }
        }

        private long GetExpiryUnixTimestamp(FileEntry[] entries, DbMount mount)
        {
            if (entries.Length == 0) return 0; // Empty buckets do not expire.
            return Utils.ToUnixTimestamp(mount.ExpiryUtc);
        }
    }

    [Serializable]
    public class Bucket
    {
        public FileEntry[] Entries { get; set; } = Array.Empty<FileEntry>();
        public ulong VolumeSize { get; set; } = 0;
        public BucketBurnState State { get; set; } = BucketBurnState.Unknown;
        public long ExpiryUtc { get; set; } = 0;
        public string RomCid { get; set; } = string.Empty;
    }
    
    [Serializable]
    public class BurnInfo
    {
        public RomInfo Fields { get; set; } = new RomInfo();
        public ulong DurabilityOptionId { get; set; }
    }
}
