using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingMicroservices.Services.PortfolioService.Data
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }

    public class PortfolioUnitOfWork : IUnitOfWork
    {
        private readonly PortfolioDbContext DbContext;
        public PortfolioUnitOfWork(PortfolioDbContext db)
        {
            DbContext = db;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return DbContext.SaveChangesAsync(ct);
        }
    }
}
