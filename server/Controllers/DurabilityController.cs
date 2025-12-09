using Microsoft.AspNetCore.Mvc;

namespace UltraDataBurningROM.Server.Controllers
{
    [ApiController]
    [Route("durability")]
    public class DurabilityController : ControllerBase
    {
        private const string DurabilityLabsSponsorLine = "Sponsored by Durability-Labs";
        private const decimal DurabilityLabsPrice = 10.0M; // USD per TB per month

        private const long Tb1 = 1L * 1024L * 1024L * 1024L * 1024L;
        private const long DaySeconds = 1L * (60L * 60L * 24L);
        private const long MonthSeconds = 1L * (DaySeconds * 30L);
        private const long Mb700 = 700L * 1024L * 1024L;

        private static readonly Durability durability = new Durability
        {
            Options =
            [
                new DurabilityOption
                {
                    Id = 1001,
                    Name = "D14-HADS",
                    PriceLine = GetPriceLine(Mb700, TimeSpan.FromDays(14.0)),
                    Description = "14-Days high-availability decentralized storage",
                    SponsorLine = DurabilityLabsSponsorLine
                },
                new DurabilityOption
                {
                    Id = 1002,
                    Name = "D30-HADS",
                    PriceLine = GetPriceLine(Mb700, TimeSpan.FromDays(30.0)),
                    Description = "30-Days high-availability decentralized storage",
                    SponsorLine = DurabilityLabsSponsorLine
                }
            ]
        };

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(durability);
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
}
