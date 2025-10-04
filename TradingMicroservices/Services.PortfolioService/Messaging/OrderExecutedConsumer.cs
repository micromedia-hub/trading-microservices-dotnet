using MassTransit;
using TradingMicroservices.Common.Contracts.Messaging;
using TradingMicroservices.Services.PortfolioService.Application;
using TradingMicroservices.Services.PortfolioService.Data;

namespace TradingMicroservices.Services.PortfolioService.Messaging
{
    /// <summary>
    /// Applies executed orders to trades and positions
    /// </summary>
    public class OrderExecutedConsumer : IConsumer<OrderExecutedEvent>
    {
        private readonly IPortfolioDomainService Service;
        private readonly IUnitOfWork UnitOfWork;
        private readonly ILogger<OrderExecutedConsumer> Logger;

        public OrderExecutedConsumer(IPortfolioDomainService service, IUnitOfWork unitOfWork, ILogger<OrderExecutedConsumer> logger)
        {
            Service = service;
            UnitOfWork = unitOfWork;
            Logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderExecutedEvent> context)
        {
            var correlationId = context.Headers.Get<string>(TradingMicroservices.Common.Constants.Messaging.Headers.CorrelationId) ?? "(none)";
            using (Serilog.Context.LogContext.PushProperty(TradingMicroservices.Common.Constants.Messaging.LogProperties.CorrelationId, correlationId))
            {
                await Service.ApplyOrderExecutionAsync(context.Message, context.CancellationToken);
                await UnitOfWork.SaveChangesAsync(context.CancellationToken);
                Logger.LogInformation("Applied OrderExecutedEvent: {OrderId} for {UserRef}", context.Message.OrderId, context.Message.UserRef);
            }
        }
    }
}
