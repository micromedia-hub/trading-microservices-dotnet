using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TradingMicroservices.Services.OrderService.Data.Entities;

namespace TradingMicroservices.Services.OrderService.Data.Repositories
{
    public interface IStockRepository
    {
        Task<Stock?> FindBySymbolAsync(string symbol, CancellationToken ct);
    }

    public class StockRepository : IStockRepository
    {
        private readonly OrderDbContext DbContext;
        public StockRepository(OrderDbContext db)
        {
            DbContext = db;
        }

        public Task<Stock?> FindBySymbolAsync(string symbol, CancellationToken ct)
        {
            return DbContext.Stocks.FirstOrDefaultAsync(s => s.Symbol == symbol, ct);
        }
    }
}
