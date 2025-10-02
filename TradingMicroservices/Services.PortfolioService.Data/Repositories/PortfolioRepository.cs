using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TradingMicroservices.Services.PortfolioService.Data.Entities;

namespace TradingMicroservices.Services.PortfolioService.Data.Repositories
{
    public interface IPortfolioRepository
    {
        Task<Position?> GetPositionAsync(string userRef, int stockId, CancellationToken ct);
        Task UpsertPositionAsync(Position position, CancellationToken ct);

        Task UpsertLastPriceAsync(LastPrice lastPrice, CancellationToken ct);
        Task<Dictionary<int, decimal>> GetLastPricesAsync(CancellationToken ct);

        Task<List<Position>> GetPositionsForUserAsync(string userRef, CancellationToken ct);

        Task AddTradeAsync(Trade trade, CancellationToken ct);
    }

    public class PortfolioRepository : IPortfolioRepository
    {
        private readonly PortfolioDbContext DbContext;
        public PortfolioRepository(PortfolioDbContext db)
        {
            DbContext = db;
        }

        public Task<Position?> GetPositionAsync(string userRef, int stockId, CancellationToken ct)
        {
            return DbContext.Positions.FirstOrDefaultAsync(x => x.UserRef == userRef && x.StockId == stockId, ct);
        }

        public async Task UpsertPositionAsync(Position position, CancellationToken ct)
        {
            if (position.Id == 0) // long bigserial
            {
                await DbContext.Positions.AddAsync(position, ct);
            }
            else
            {
                DbContext.Positions.Update(position);
            }
        }

        public async Task UpsertLastPriceAsync(LastPrice lastPrice, CancellationToken ct)
        {
            var existing = await DbContext.LastPrices.FindAsync(new object?[] { lastPrice.StockId }, ct);
            if (existing is null)
            {
                await DbContext.LastPrices.AddAsync(lastPrice, ct);
            }
            else
            {
                existing.Price = lastPrice.Price;
                existing.UpdateDate = lastPrice.UpdateDate;
                DbContext.LastPrices.Update(existing);
            }
        }

        public async Task<Dictionary<int, decimal>> GetLastPricesAsync(CancellationToken ct)
        {
            return await DbContext.LastPrices.ToDictionaryAsync(x => x.StockId, x => x.Price, ct);
        }

        public Task<List<Position>> GetPositionsForUserAsync(string userRef, CancellationToken ct)
        {
            return DbContext.Positions.Include(p => p.Stock).Where(x => x.UserRef == userRef).ToListAsync(ct);
        }
        public Task AddTradeAsync(Trade trade, CancellationToken ct)
        {
            return DbContext.Trades.AddAsync(trade, ct).AsTask();
        }
    }
}
