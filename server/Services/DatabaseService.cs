using Newtonsoft.Json;

namespace UltraDataBurningROM.Server.Services
{
    public interface IDatabaseService
    {
        T? Get<T>(string id) where T : DbEntity;
        void Save<T>(T entity) where T : DbEntity;
        void Iterate<T>(Action<T> onEntity) where T : DbEntity;
        void Delete<T>(string id) where T : DbEntity;
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly Lock _lock = new Lock();
        private readonly string rootPath = "/database";
        private readonly CapMap<string, CapMap<string, object>> cache = new CapMap<string, CapMap<string, object>>();

        public DatabaseService()
        {
            CreateDirectory(rootPath);
            CreateDirectory(Path.Combine(rootPath, typeof(DbUser).Name.ToLowerInvariant()));
            CreateDirectory(Path.Combine(rootPath, typeof(DbMount).Name.ToLowerInvariant()));
            CreateDirectory(Path.Combine(rootPath, typeof(DbRom).Name.ToLowerInvariant()));
            CreateDirectory(Path.Combine(rootPath, typeof(DbPopContent).Name.ToLowerInvariant()));
        }

        public T? Get<T>(string id) where T : DbEntity
        {
            lock (_lock)
            {
                var typename = typeof(T).Name.ToLowerInvariant();
                var cached = GetFromCache<T>(typename, id);
                if (cached != null) return cached;

                var filename = Path.Combine(rootPath, typename, id);
                try
                {
                    var entity = JsonConvert.DeserializeObject<T>(File.ReadAllText(filename));
                    if (entity != null) AddUpdateCache(typename, id, entity);
                    return entity;
                }
                catch
                {
                    File.Delete(filename);
                }
                return null;
            }
        }

        public void Save<T>(T entity) where T : DbEntity
        {
            lock (_lock)
            {
                var typename = typeof(T).Name.ToLowerInvariant();
                AddUpdateCache(typename, entity.Id, entity);
                var filename = Path.Combine(rootPath, typename, entity.Id);
                try
                {
                    File.WriteAllText(filename, JsonConvert.SerializeObject(entity));
                }
                catch
                {
                }
            }
        }

        public void Iterate<T>(Action<T> onEntity) where T : DbEntity
        {
            // We don't lock while iterating. Instead, we take a snapshot of all the 
            // known entries for this type. Then we try to read them from the cache
            // if that fails we try to read them from the disk, and if that fails
            // we ignore it.

            try
            {
                var typename = typeof(T).Name.ToLowerInvariant();
                var map = GetMap(typename);
                var typeRoot = Path.Combine(rootPath, typename);
                var files = Directory.GetFiles(typeRoot);
                foreach (var filepath in files)
                {
                    try
                    {
                        PassEntity(filepath, map, onEntity);
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception iterating types: " + e);
            }
        }

        public void Delete<T>(string id) where T : DbEntity
        {
            lock (_lock)
            {
                var typename = typeof(T).Name.ToLowerInvariant();
                var map = GetMap(typename);
                map.ClearKey(id);

                var filename = Path.Combine(rootPath, typename, id);
                try
                {
                    if (File.Exists(filename)) File.Delete(filename);
                }
                catch
                {
                }
            }
        }

        private void PassEntity<T>(string filepath, CapMap<string, object> map, Action<T> onEntity) where T : DbEntity
        {
            var filename = Path.GetFileName(filepath);
            if (map.TryGetValue(filename, out var entity))
            {
                if (entity is T fromCache)
                {
                    onEntity(fromCache);
                    return;
                }
            }
            var fromDisk = JsonConvert.DeserializeObject<T>(File.ReadAllText(filepath));
            if (fromDisk != null)
            {
                onEntity(fromDisk);
            }
        }

        private void CreateDirectory(string rootPath)
        {
            Directory.CreateDirectory(rootPath);
        }

        private void AddUpdateCache<T>(string typename, string id, T entity) where T : DbEntity
        {
            var map = GetMap(typename);
            map.Set(id, entity);
        }

        private CapMap<string, object> GetMap(string typename)
        {
            if (!cache.ContainsKey(typename)) cache.Add(typename, new CapMap<string, object>());
            return cache.Get(typename);
        }

        private T? GetFromCache<T>(string typename, string id) where T : DbEntity
        {
            var map = GetMap(typename);
            if (map.TryGetValue(id, out object? obj))
            {
                return (T?)obj;
            }
            return null;
        }
    }

    public abstract class DbEntity
    {
        public string Id { get; set; } = string.Empty;
    }

    [Serializable]
    public class DbUser : DbEntity
    {
        public string Username { get; set; } = string.Empty;
        public string BucketMountId { get; set; } = string.Empty;
        public BucketBurnState BucketBurnState { get; set; } = BucketBurnState.Unknown;
        public string BucketNewRomCid { get; set; } = string.Empty;
    }

    [Serializable]
    public enum BucketBurnState
    {
        Unknown,
        Open,
        Starting,
        Compressing,
        Uploading,
        Purchasing,
        Done
    }

    [Serializable]
    public class DbMount : DbEntity
    {
        public string Path { get; set; } = string.Empty;
        public DateTime ExpiryUtc { get; set; }
        public MountState State { get; set; } = MountState.Unknown;
    }
    
    [Serializable]
    public enum MountState
    {
        Unknown,
        Bucket,
        Downloading,
        OpenInUse,
        ClosedNotUsed
    }

    [Serializable]
    public class DbRom : DbEntity
    {
        public string RomCid { get => Id; set => Id = value; }
        public RomInfo Info { get; set; } = new RomInfo();
        public FileEntry[] Files { get; set; } = Array.Empty<FileEntry>();
        public DateTime StorageExpireUtc { get; set; }
        public int MountCounter;
        public string CurrentMountId { get; set; } = string.Empty;
    }

    [Serializable]
    public class RomInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    [Serializable]
    public class FileEntry
    {
        public string Filename { get; set; } = string.Empty;
        public ulong ByteSize { get; set; } = 0;
    }

    [Serializable]
    public class DbPopContent : DbEntity
    {
        public string[] RomCids { get; set; } = Array.Empty<string>();
        public string[] Tags { get; set; } = Array.Empty<string>();
    }
}
