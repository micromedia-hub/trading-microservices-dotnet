using MassTransit;
using TradingMicroservices.Common.Contracts.Messaging;

namespace TradingMicroservices.Services.OrderService.Application
{
    /// <summary>
    /// Publishes domain/integration events after DB commit.
    /// </summary>
    public interface IOrderEventPublisher
    {
        Task PublishOrderExecutedAsync(OrderExecutedEvent result, string correlationId, CancellationToken ct);
    }

    public class OrderEventPublisher : IOrderEventPublisher
    {
        private readonly IPublishEndpoint Publisher;

        public OrderEventPublisher(IPublishEndpoint publisher)
        {
            Publisher = publisher;
        }

        public Task PublishOrderExecutedAsync(OrderExecutedEvent result, string correlationId, CancellationToken ct)
        {
            return Publisher.Publish(result, send => 
            {
                if (!string.IsNullOrWhiteSpace(correlationId))
                {
                    send.Headers.Set(TradingMicroservices.Common.Constants.Messaging.Headers.CorrelationId, correlationId);
                }
            }, ct);
        }
    }
}
