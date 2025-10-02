using MassTransit;
using TradingMicroservices.Common.Contracts.Messaging;

namespace TradingMicroservices.Services.OrderService.Application
{
    /// <summary>
    /// Publishes domain/integration events after DB commit.
    /// </summary>
    public interface IOrderEventPublisher
    {
        Task PublishOrderExecutedAsync(OrderExecutedEvent result, CancellationToken ct);
    }

    public class OrderEventPublisher : IOrderEventPublisher
    {
        private readonly IPublishEndpoint Publisher;

        public OrderEventPublisher(IPublishEndpoint publisher)
        {
            Publisher = publisher;
        }

        public Task PublishOrderExecutedAsync(OrderExecutedEvent result, CancellationToken ct)
        {
            var evt = new OrderExecutedEvent
            {
                OrderId = result.OrderId,
                UserRef = result.UserRef,
                StockSymbol = result.StockSymbol,
                FilledQuantity = result.FilledQuantity,
                FillPrice = result.FillPrice,
                Date = result.Date,
                TraceId = Guid.NewGuid().ToString()
            };

            return Publisher.Publish(evt, ct);
        }
    }
}
