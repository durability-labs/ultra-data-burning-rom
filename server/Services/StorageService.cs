namespace UltraDataBurningROM.Server.Services
{
    public interface IStorageService
    {
        void Initialize();
        Durability GetDurability();
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

        public StorageService()
        {
            durability = new Durability
            {
                Options = config.Select(c => c.Representation).ToArray()
            };
        }

        public void Initialize()
        {
            // ping nodes, all OK?
        }

        public Durability GetDurability()
        {
            return durability;
        }

        public string Upload(string filepath)
        {
            return "todo_uploadcid_" + filepath;
        }

        public PurchaseResponse PurchaseStorage(string cid, ulong optionId)
        {
            throw new NotImplementedException();
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
