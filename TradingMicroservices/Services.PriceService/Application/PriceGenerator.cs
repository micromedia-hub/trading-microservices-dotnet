using MassTransit;
using TradingMicroservices.Common.Contracts.Messaging;

namespace TradingMicroservices.Services.PriceService.Application
{
    /// <summary>
    /// Generates random prices for specified symbols and publishes a PriceUpdatedEvent via MassTransit.
    /// </summary>
    public class PriceGenerator : BackgroundService
    {
        private readonly IBus Bus;
        private readonly ILogger<PriceGenerator> Logger;
        private readonly IConfiguration Config;

        private static readonly string[] Symbols = { "AAPL", "TSLA", "NVDA", "MSFT", "AMZN" };

        private readonly Dictionary<string, decimal> LastPriceDictionary = new();
        private readonly Random RandomGenerator = new();

        public PriceGenerator(IBus bus, ILogger<PriceGenerator> logger, IConfiguration config)
        {
            Bus = bus;
            Logger = logger;
            Config = config;
            // Initial price
            foreach (var symbol in Symbols)
            {
                // Price between [100..150]
                LastPriceDictionary[symbol] = 100m + (decimal)RandomGenerator.NextDouble() * 50m;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var interval = Config.GetValue<int?>("Prices:Interval") ?? 1000;
            var min = Config.GetValue<decimal?>("Prices:Min") ?? 50m;
            var max = Config.GetValue<decimal?>("Prices:Max") ?? 500m;
            Logger.LogInformation("Price generator started: interval={Interval}ms symbols=[{Symbols}]", interval, string.Join(",", Symbols));
            while (!ct.IsCancellationRequested)
            {
                foreach (var symbol in Symbols)
                {
                    var previousPrice = LastPriceDictionary[symbol];
                    var delta = (decimal)(RandomGenerator.NextDouble() - 0.5) * 2m; // -1..+1
                    var price = Math.Clamp(LastPriceDictionary[symbol] + delta, min, max);
                    LastPriceDictionary[symbol] = price;
                    var direction = price > previousPrice ? "↑" : price < previousPrice ? "↓" : "→";
                    var signedDelta = price - previousPrice;
                    var eventData = new PriceUpdatedEvent
                    {
                        StockSymbol = symbol,
                        Price = decimal.Round(price, 4),
                        Timestamp = DateTimeOffset.UtcNow,
                        TraceId = Guid.NewGuid().ToString()
                    };
                    await Bus.Publish(eventData, ct);
                    Logger.LogInformation("PriceUpdated: {Symbol}={Price}{Direction} (Δ{Delta})",
                        symbol, eventData.Price, direction, decimal.Round(signedDelta, 4));
                }
                try
                {
                    await Task.Delay(interval, ct);
                }
                catch (TaskCanceledException)
                {
                    /* shutting down */
                }
            }
            Logger.LogInformation("Price generator stopping.");
        }
    }
}
