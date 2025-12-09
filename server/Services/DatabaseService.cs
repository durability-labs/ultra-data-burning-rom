using Newtonsoft.Json;

namespace UltraDataBurningROM.Server.Services
{
    public interface IDatabaseService
    {
        T? Get<T>(string id) where T : DbEntity;
        void Save<T>(T entity) where T : DbEntity;
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly object _lock = new object();
        private readonly string rootPath = "/database";
        private readonly CapMap<string, CapMap<string, object>> cache = new CapMap<string, CapMap<string, object>>();

        public DatabaseService()
        {
            Directory.CreateDirectory(rootPath);
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
                    return JsonConvert.DeserializeObject<T>(File.ReadAllText(filename));
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

        private void AddUpdateCache<T>(string typename, string id, T entity) where T : DbEntity
        {
            if (!cache.ContainsKey(typename)) cache.Add(typename, new CapMap<string, object>());
            var map = cache.Get(typename);
            map.Set(id, entity);
        }

        private T? GetFromCache<T>(string typename, string id) where T : DbEntity
        {
            if (cache.TryGetValue(typename, out CapMap<string, object>? map))
            {
                if (map != null && map.TryGetValue(id, out object? obj))
                {
                    return (T?)obj;
                }
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
        public int BucketBurnState { get; set; } = 0;
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
}
