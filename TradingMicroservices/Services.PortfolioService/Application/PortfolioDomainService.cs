using TradingMicroservices.Common.Contracts.Messaging;
using TradingMicroservices.Services.PortfolioService.Data.Entities;
using TradingMicroservices.Services.PortfolioService.Data.Repositories;

namespace TradingMicroservices.Services.PortfolioService.Application
{
    /// <summary>
    /// Domain operations to apply executions and price ticks to the portfolio state.
    /// </summary>
    public interface IPortfolioDomainService
    {
        Task ApplyOrderExecutionAsync(OrderExecutedEvent message, CancellationToken ct);
        Task ApplyPriceTickAsync(PriceUpdatedEvent message, bool useSql, CancellationToken ct);
    }

    public class PortfolioDomainService : IPortfolioDomainService
    {
        private readonly IPortfolioRepository PortfolioRepository;
        private readonly IStockRepository StockRepository;
        private readonly ILogger<PortfolioDomainService> Logger;

        public PortfolioDomainService(IPortfolioRepository portfolioRepository,
            IStockRepository stockRepository, ILogger<PortfolioDomainService> logger)
        {
            PortfolioRepository = portfolioRepository;
            StockRepository = stockRepository;
            Logger = logger;
        }

        public async Task ApplyOrderExecutionAsync(OrderExecutedEvent message, CancellationToken ct)
        {
            var stock = await StockRepository.FindBySymbolAsync(message.StockSymbol, ct);
            if (stock is null)
            {
                Logger.LogWarning("Unknown stock symbol {Symbol} in OrderExecutedEvent {OrderId}", message.StockSymbol, message.OrderId);
                return;
            }
            // 1) Create Trade
            var trade = new Trade
            {
                Id = Guid.NewGuid(),
                OrderRefId = message.OrderId,
                UserRef = message.UserRef,
                StockId = stock.Id,
                Quantity = message.FilledQuantity,
                Price = message.FillPrice,
                Date = message.Date
            };
            await PortfolioRepository.AddTradeAsync(trade, ct);
            // 2) Upsert Position
            var position = await PortfolioRepository.GetPositionAsync(message.UserRef, stock.Id, ct);
            var now = DateTimeOffset.UtcNow;
            if (position is null)
            {
                if (message.FilledQuantity > 0)
                {
                    // BUY
                    position = new Position
                    {
                        UserRef = message.UserRef,
                        StockId = stock.Id,
                        Quantity = message.FilledQuantity,
                        AvgPrice = Math.Round(message.FillPrice, 4),
                        RealizedPnl = 0m,
                        UpdateDate = now
                    };
                    await PortfolioRepository.UpsertPositionAsync(position, ct);
                }
                else
                {
                    // SELL without a position
                    Logger.LogWarning("SELL without existing position: User={UserRef}, Symbol={Symbol}, Qty={Qty}",
                        message.UserRef, message.StockSymbol, message.FilledQuantity);
                }
                return;
            }
            if (message.FilledQuantity > 0)
            {
                // BUY
                var newQuantity = position.Quantity + message.FilledQuantity;
                // Calculate average price
                position.AvgPrice = Math.Round(
                    (position.Quantity * position.AvgPrice + message.FilledQuantity * message.FillPrice) / newQuantity, 4);
                position.Quantity = newQuantity;
                position.UpdateDate = now;
                await PortfolioRepository.UpsertPositionAsync(position, ct);
            }
            else if (message.FilledQuantity < 0)
            {
                // SELL: realize PnL on the portion they actually hold
                // Convert to positive to simplify calculations
                var sellQuantity = -message.FilledQuantity;
                var heldQuantity = position.Quantity;
                var quantityToClose = Math.Min(heldQuantity, sellQuantity);
                if (quantityToClose > 0)
                {
                    position.RealizedPnl = Math.Round(position.RealizedPnl + (message.FillPrice - position.AvgPrice) * quantityToClose, 4);
                }
                var newQuantity = position.Quantity - sellQuantity;
                if (newQuantity <= 0)
                {
                    // Position fully closed
                    position.Quantity = 0;
                    position.AvgPrice = 0;
                }
                else
                {
                    position.Quantity = newQuantity;
                    // position.AvgPrice does not change here
                }
                position.UpdateDate = now;
                await PortfolioRepository.UpsertPositionAsync(position, ct);
            }
        }

        public async Task ApplyPriceTickAsync(PriceUpdatedEvent message, bool useSql, CancellationToken ct)
        {
            var stock = await StockRepository.FindBySymbolAsync(message.StockSymbol, ct);
            if (stock is null)
            {
                Logger.LogWarning("Unknown stock symbol {Symbol} in PriceUpdatedEvent", message.StockSymbol);
                return;
            }
            var lastPrice = new LastPrice
            {
                StockId = stock.Id,
                Price = Math.Round(message.Price, 4),
                UpdateDate = message.Timestamp
            };
            if (useSql)
            {
                await PortfolioRepository.UpsertLastPriceSqlAsync(lastPrice, ct);
            }
            else
            {
                await PortfolioRepository.UpsertLastPriceAsync(lastPrice, ct);
            }            
        }
    }
}
