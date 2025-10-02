using MassTransit;
using TradingMicroservices.Common.Contracts.Messaging;
using TradingMicroservices.Services.PortfolioService.Application;
using TradingMicroservices.Services.PortfolioService.Data;

namespace TradingMicroservices.Services.PortfolioService.Messaging
{
    /// <summary>
    /// Applies last price updates then commits.
    /// </summary>
    public class PriceUpdatedConsumer : IConsumer<PriceUpdatedEvent>
    {
        private readonly IPortfolioDomainService Service;
        private readonly IUnitOfWork UnitOfWork;
        private readonly ILogger<PriceUpdatedConsumer> Logger;

        public PriceUpdatedConsumer(IPortfolioDomainService service, IUnitOfWork unitOfWork, ILogger<PriceUpdatedConsumer> logger)
        {
            Service = service;
            UnitOfWork = unitOfWork;
            Logger = logger;
        }

        public async Task Consume(ConsumeContext<PriceUpdatedEvent> context)
        {
            await Service.ApplyPriceTickAsync(context.Message, context.CancellationToken);
            await UnitOfWork.SaveChangesAsync(context.CancellationToken);
            Logger.LogDebug("Applied PriceUpdatedEvent: {Symbol}={Price}", context.Message.StockSymbol, context.Message.Price);
        }
    }
}
