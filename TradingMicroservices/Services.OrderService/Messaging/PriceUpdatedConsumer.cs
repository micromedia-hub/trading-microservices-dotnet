using MassTransit;
using TradingMicroservices.Common.Contracts.Messaging;
using TradingMicroservices.Services.OrderService.Infrastructure;

namespace TradingMicroservices.Services.OrderService.Messaging
{
    /// <summary>
    /// Updates the in-memory price cache whenever a new price arrives.
    /// </summary>
    public class PriceUpdatedConsumer : IConsumer<PriceUpdatedEvent>
    {
        private readonly IPriceCache PriceCache;
        private readonly ILogger<PriceUpdatedConsumer> Logger;

        public PriceUpdatedConsumer(IPriceCache priceCache, ILogger<PriceUpdatedConsumer> logger)
        {
            PriceCache = priceCache;
            Logger = logger;
        }

        public Task Consume(ConsumeContext<PriceUpdatedEvent> context)
        {
            PriceCache.Set(context.Message.StockSymbol, context.Message.Price);
            Logger.LogDebug("Cached price: {Symbol}={Price}", context.Message.StockSymbol, context.Message.Price);
            return Task.CompletedTask;
        }
    }
}
