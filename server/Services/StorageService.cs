using ArchivistClient;

namespace UltraDataBurningROM.Server.Services
{
    public interface IStorageService
    {
        void Initialize();
        Durability GetDurability();
        IStorageNode TakeNode();
        void ReleaseNode(IStorageNode node);
    }

    public interface IStorageNode
    {
        string Upload(string filepath);
        PurchaseResponse PurchaseStorage(string cid, ulong optionId);
    }

    public class StorageService : IStorageService
    {
        private const string DurabilityLabsSponsorLine = "Sponsored by Durability-Labs";
        private const decimal DurabilityLabsPrice = 10.0M; // USD per TB per month

        private const long Tb1 = 1L * 1024L * 1024L * 1024L * 1024L;
        private const long DaySeconds = 1L * (60L * 60L * 24L);
        private const long MonthSeconds = 1L * (DaySeconds * 30L);
        private const long Mb700 = 700L * 1024L * 1024L;

        private readonly DurabilityConfig[] config =
        [
            new DurabilityConfig
            {
                Representation = new DurabilityOption
                {
                    Id = 1001,
                    Name = "D14-HADS",
                    PriceLine = GetPriceLine(Mb700, TimeSpan.FromDays(14.0)),
                    Description = "14-Days high-availability decentralized storage",
                    SponsorLine = DurabilityLabsSponsorLine
                },
                Nodes = 6,
                Tolerance = 3,
                Duration = TimeSpan.FromDays(14.0),
                Expiry = TimeSpan.FromMinutes(30.0),
                PricePerBytePerSecond = 1000,
                CollateralPerByte = 1,
                ProofProbability = 244
            },
            new DurabilityConfig
            {
                Representation = new DurabilityOption
                {
                    Id = 1002,
                    Name = "D30-HADS",
                    PriceLine = GetPriceLine(Mb700, TimeSpan.FromDays(30.0)),
                    Description = "30-Days high-availability decentralized storage",
                    SponsorLine = DurabilityLabsSponsorLine
                },
                Nodes = 6,
                Tolerance = 3,
                Duration = TimeSpan.FromDays(29.98), // default max is exactly 30d. Make sure we're under that.
                Expiry = TimeSpan.FromMinutes(30.0),
                PricePerBytePerSecond = 1000,
                CollateralPerByte = 1,
                ProofProbability = 244
            },
        ];

        private readonly Durability durability;
        private readonly List<StorageNode> nodes = new List<StorageNode>();
        private static readonly Lock _nodesLock = new Lock();

        public StorageService()
        {
            durability = new Durability
            {
                Options = config.Select(c => c.Representation).ToArray()
            };
        }

        public void Initialize()
        {
            var endpoints = EnvConfig.ArchivistEndpoints;
            nodes.AddRange(
                endpoints.Select(e =>
                {
                    Console.WriteLine("Pinging Archivist node at: " + e);
                    var instance = new ArchivistInstance(e);

                    while (!instance.Ping())
                    {
                        Console.WriteLine("Ping...");
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }

                    return new StorageNode(
                        instance,
                        config
                    );
                })
            );
            Console.WriteLine("Storage service initialized.");
        }

        public Durability GetDurability()
        {
            return durability;
        }

        public IStorageNode TakeNode()
        {
            while (true)
            {
                lock (_nodesLock)
                {
                    var all = nodes.ToArray();
                    foreach (var n in all)
                    {
                        if (!n.InUse)
                        {
                            n.InUse = true;
                            // Move to the back of the list.
                            nodes.Remove(n);
                            nodes.Add(n);
                            return n;
                        }
                    }
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        public void ReleaseNode(IStorageNode node)
        {
            lock (_nodesLock)
            {
                var n = (StorageNode)node;
                n.InUse = false;
            }
        }

        private static string GetPriceLine(long bytes, TimeSpan timeSpan)
        {
            var price = GetPriceUsd(bytes, timeSpan);

            return $"Price: ${price} (sponsor discount: 100%)";
        }

        private static decimal GetPriceUsd(long bytes, TimeSpan timeSpan)
        {
            decimal pricePerTbPerMonth = DurabilityLabsPrice;
            decimal pricePerBytePerMonth = pricePerTbPerMonth / ((decimal)Tb1);
            decimal pricePerBytePerSecond = pricePerBytePerMonth / ((decimal)MonthSeconds);
            decimal b = bytes;
            decimal seconds = Convert.ToDecimal(timeSpan.TotalSeconds);

            return pricePerBytePerSecond * b * seconds;
        }
    }

    public class StorageNode : IStorageNode
    {
        private readonly ArchivistInstance instance;
        private readonly DurabilityConfig[] config;

        public bool InUse { get; set; } = false;

        public StorageNode(ArchivistInstance instance, DurabilityConfig[] config)
        {
            this.instance = instance;
            this.config = config;
        }

        public string Upload(string filepath)
        {
            return instance.UploadFile(filepath);
        }

        public PurchaseResponse PurchaseStorage(string cid, ulong optionId)
        {
            var selected = config.Single(c => c.Representation.Id == optionId);

            var purchaseId = instance.PurchaseStorage(cid,
                selected.Nodes,
                selected.Tolerance,
                selected.Duration,
                selected.Expiry,
                selected.PricePerBytePerSecond,
                selected.CollateralPerByte,
                selected.ProofProbability
            );

            if (!instance.WaitForPurchaseStarted(purchaseId, selected.Expiry))
            {
                throw new Exception("Failed to start purchase");
            }
            var startUtc = DateTime.UtcNow - TimeSpan.FromSeconds(30.0);
            var purchaseCid = instance.GetPurchaseCid(purchaseId);
            var finishUtc = startUtc + selected.Duration;

            return new PurchaseResponse
            {
                FinishUtc = finishUtc,
                PurchaseCid = purchaseCid,
            };
        }
    }

    public class PurchaseResponse
    {
        public string PurchaseCid { get; set; } = string.Empty;
        public DateTime FinishUtc { get; set; }
    }

    public class Durability
    {
        public DurabilityOption[] Options { get; set; } = Array.Empty<DurabilityOption>();
    }

    public class DurabilityOption
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PriceLine { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SponsorLine { get; set; } = string.Empty;
    }

    public class DurabilityConfig
    {
        public DurabilityOption Representation { get; set; } = new DurabilityOption();
        public int Nodes { get; set; } = 0;
        public int Tolerance { get; set; } = 0;
        public TimeSpan Duration { get; set; }
        public TimeSpan Expiry { get; set; }
        public ulong PricePerBytePerSecond { get; set; } = 0;
        public ulong CollateralPerByte { get; set; } = 0;
        public int ProofProbability { get; set; } = 0;
    }
}
