using ArchivistClient;

namespace UltraDataBurningROM.Server.Services
{
    public interface IStorageService
    {
        void Initialize(ILogger<StorageService> logger);
        Durability GetDurability();
        IStorageNode TakeNode();
        void ReleaseNode(IStorageNode node);
    }

    public interface IStorageNode
    {
        string Upload(string filepath);
        PurchaseResponse PurchaseStorage(string cid, ulong optionId);
        void Download(string cid, string filepath);
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
                    Id = 1003,
                    Name = "D6-HADS",
                    PriceLine = GetPriceLine(Mb700, TimeSpan.FromDays(6.0)),
                    Description = "6-Days high-availability decentralized storage",
                    SponsorLine = DurabilityLabsSponsorLine
                },
                Nodes = 6,
                Tolerance = 3,
                Duration = TimeSpan.FromDays(6.0),
                Expiry = TimeSpan.FromMinutes(30.0),
                PricePerBytePerSecond = 1000,
                CollateralPerByte = 1,
                ProofProbability = 244
            },
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
        private ILogger logger = null!;

        public StorageService()
        {
            durability = new Durability
            {
                Options = config.Select(c => c.Representation).ToArray()
            };
        }

        public void Initialize(ILogger<StorageService> logger)
        {
            this.logger = logger;
            var endpoints = EnvConfig.ArchivistEndpoints;
            nodes.AddRange(
                endpoints.Select(e =>
                {
                    logger.LogInformation("Pinging Archivist node at: {e}", e);
                    var instance = new ArchivistInstance(msg => logger.LogInformation(msg), e);

                    while (!instance.Ping())
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }

                    logger.LogInformation("OK: {e}", e);
                    return new StorageNode(
                        logger,
                        instance,
                        config
                    );
                })
            );
            logger.LogInformation("Storage service initialized.");
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
                            logger.LogInformation("Node in use");
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
                logger.LogInformation("Node released");
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
        private readonly ILogger logger;
        private readonly ArchivistInstance instance;
        private readonly DurabilityConfig[] config;

        public bool InUse { get; set; } = false;

        public StorageNode(ILogger logger, ArchivistInstance instance, DurabilityConfig[] config)
        {
            this.logger = logger;
            this.instance = instance;
            this.config = config;
        }

        public string Upload(string filepath)
        {
            logger.LogInformation("Uploading file {filepath}", filepath);
            var cid = instance.UploadFile(filepath);
            logger.LogInformation("Uploaded file {filepath} to {cid}", filepath, cid);
            return cid;
        }

        public PurchaseResponse PurchaseStorage(string cid, ulong optionId)
        {
            logger.LogInformation("Purchasing storage for {cid} with option {optionId}", cid, optionId);
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

            logger.LogInformation("Purchase for {cid} with option {optionId} yielded {purchaseId}", cid, optionId, purchaseId);

            logger.LogInformation("Waiting for {purchaseId} to start...", purchaseId);
            if (!instance.WaitForPurchaseStarted(purchaseId, selected.Expiry))
            {
                throw new Exception("Failed to start purchase");
            }
            var startUtc = DateTime.UtcNow - TimeSpan.FromSeconds(30.0);
            var purchaseCid = instance.GetPurchaseCid(purchaseId);
            var finishUtc = startUtc + selected.Duration;

            logger.LogInformation("Purchase {purchaseId} successfully started.", purchaseId);

            return new PurchaseResponse
            {
                FinishUtc = finishUtc,
                PurchaseCid = purchaseCid,
            };
        }

        public void Download(string cid, string filepath)
        {
            instance.Download(cid, filepath);
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
