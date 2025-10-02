using TradingMicroservices.Common.Contracts.Http;
using TradingMicroservices.Common.Contracts.Messaging;
using TradingMicroservices.Common.Enums;
using TradingMicroservices.Services.OrderService.Data;
using TradingMicroservices.Services.OrderService.Data.Entities;
using TradingMicroservices.Services.OrderService.Data.Repositories;
using TradingMicroservices.Services.OrderService.Infrastructure;

namespace TradingMicroservices.Services.OrderService.Application
{
    /// <summary>
    /// Handles placing an order: validate, lookup stock, take last price, persist Order and Execution, commit.
    /// </summary>
    public interface IOrderProcessingService
    {
        Task<OrderExecutedEvent> PlaceOrderAsync(PlaceOrderRequest request, string userRef, CancellationToken ct);
    }

    public class OrderProcessingService : IOrderProcessingService
    {
        private readonly IStockRepository stockRepository;
        private readonly IOrderRepository orderRepository;
        private readonly IUnitOfWork unitOfWork;
        private readonly IPriceCache priceCache;

        public OrderProcessingService(IStockRepository stocks, IOrderRepository orders, IUnitOfWork uow, IPriceCache prices)
        {
            stockRepository = stocks;
            orderRepository = orders;
            unitOfWork = uow;
            priceCache = prices;
        }

        public async Task<OrderExecutedEvent> PlaceOrderAsync(PlaceOrderRequest request, string userRef, CancellationToken ct)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(userRef))
            {
                throw new InvalidOperationException("UserRef is missing.");
            }
            if (string.IsNullOrWhiteSpace(request.StockSymbol))
            {
                throw new ArgumentException("StockSymbol is required.", nameof(request.StockSymbol));
            }
            if (request.Quantity <= 0)
            {
                throw new ArgumentException("Quantity must be > 0.", nameof(request.Quantity));
            }
            if (!Enum.IsDefined(typeof(OrderSide), request.Side))
            {
                throw new ArgumentException("Invalid Side.", nameof(request.Side));
            }
            var stock = await stockRepository.FindBySymbolAsync(request.StockSymbol.Trim(), ct);
            if (stock is null)
            {
                throw new InvalidOperationException($"Unknown stock symbol '{request.StockSymbol}'.");
            }
            if (!priceCache.TryGet(stock.Symbol, out var lastPrice))
            {
                throw new InvalidOperationException($"No price available for '{stock.Symbol}'. Try again later.");
            }
            // Save to DB
            var now = DateTimeOffset.UtcNow;
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserRef = userRef,
                StockId = stock.Id,
                Quantity = request.Quantity,
                Side = request.Side,
                Date = now
            };
            // SELL uses negative filled quantity
            var signedQuantity = request.Side == OrderSide.Buy ? request.Quantity : -request.Quantity;
            order.Execution = new OrderExecution
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FillPrice = decimal.Round(lastPrice, 4),
                FilledQuantity = signedQuantity,
                Date = now
            };
            await orderRepository.AddAsync(order, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return new OrderExecutedEvent
            {
                OrderId = order.Id,
                UserRef = userRef,
                StockSymbol = stock.Symbol,
                FilledQuantity = signedQuantity,
                FillPrice = order.Execution.FillPrice,
                Date = order.Execution.Date,
                TraceId = Guid.NewGuid().ToString()
            };
        }
    }
}
