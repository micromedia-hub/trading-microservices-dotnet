namespace TradingMicroservices.Services.PortfolioService.Messaging
{
    public class PriceUpdatedProcessingOptions
    {
        public bool UseSqlUpsert { get; set; } = true;
        /// <summary>
        /// "Partitioned" | "Serialized" | "Parallel"
        /// </summary>
        public string ConcurrencyMode { get; set; } = "Partitioned";
        public int Partitions { get; set; } = 8;
        public int RetryCount { get; set; } = 3;
        public int RetryIntervalMilliseconds { get; set; } = 250;
    }
}
